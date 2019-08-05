using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public abstract class HangfireTenantsInMemoryCachingStore : IHangfireTenantsStore
    {
        private readonly IAppCache _cache;

        public virtual bool CachingEnabled { get; set; } = true;
        public virtual int CacheExpiryMinutes { get; set; } = 20;

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public HangfireTenantsInMemoryCachingStore(ILogger logger, IServiceProvider serviceProvider)
        {
            _cache = new CachingService();
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public abstract Task<IEnumerable<HangfireTenant>> GetAllActiveTenantsFromStoreAsync(CancellationToken cancellationToken = default);

        private static List<HangfireTenant> _activeTenants = new List<HangfireTenant>();
        public virtual async Task<IEnumerable<HangfireTenant>> GetAllTenantsAsync(bool waitForInitialization = false, CancellationToken cancellationToken = default)
        {
            Func<Task<IEnumerable<HangfireTenant>>> getAllActiveTenantsFactory = async () => {
                //Retrieve Active tenants from store.
                var allTenants = (await GetAllActiveTenantsFromStoreAsync(cancellationToken).ConfigureAwait(false)).Where(t => t.Active);

                Func<IServiceProvider, CancellationToken, Task> initializeTenants = async (serviceProvider, token) =>
                {
                    var hangfireTenantSetup = serviceProvider.GetService<IHangfireTenantSetup>();

                    if (hangfireTenantSetup != null)
                    {
                        var previousTenantIds = _activeTenants?.Select(t => t.Id) ?? Enumerable.Empty<string>();
                        var activeTenantIds = allTenants?.Select(t => t.Id) ?? Enumerable.Empty<string>();

                        var existingTenants = _activeTenants?.Where(t => activeTenantIds.Contains(t.Id)).ToList() ?? Enumerable.Empty<HangfireTenant>();
                        var addedTenants = allTenants.Where(t => !previousTenantIds.Contains(t.Id)).ToList();
                        var removedTenants = _activeTenants?.Where(t => !activeTenantIds.Contains(t.Id)).ToList() ?? Enumerable.Empty<HangfireTenant>();

                        foreach (var removedTenant in removedTenants)
                        {
                            if (!token.IsCancellationRequested)
                            {
                                await hangfireTenantSetup.OnTenantRemoved(removedTenant).ConfigureAwait(false);
                                _activeTenants.RemoveAll(t => t.Id == removedTenant.Id);
                            }
                        }

                        foreach (var addedTenant in addedTenants)
                        {
                            if (!token.IsCancellationRequested)
                            {
                                await hangfireTenantSetup.OnTenantAdded(addedTenant).ConfigureAwait(false);
                                _activeTenants.Add(addedTenant);
                            }
                        }

                        foreach (var existingTenant in existingTenants)
                        {
                            if (!token.IsCancellationRequested)
                            {
                                var updatedTenant = allTenants.First(t => t.Id == existingTenant.Id);
                                await hangfireTenantSetup.OnTenantUpdated(updatedTenant, existingTenant).ConfigureAwait(false);
                                _activeTenants[_activeTenants.IndexOf(existingTenant)] = updatedTenant;
                            }
                        }
                    }

                    _logger.LogInformation($"Hangfire Initialization is complete");
                };

                if (waitForInitialization)
                {
                    await initializeTenants(_serviceProvider, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var applicationLifetime = _serviceProvider.GetRequiredService<IApplicationLifetime>();
                    var serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
                    _ = Task.Run(async () => {
                        try
                        {
                            using(var scope = serviceScopeFactory.CreateScope())
                            {
                                await initializeTenants(scope.ServiceProvider, applicationLifetime.ApplicationStopping).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Hangfire Initialization Failed");
                        }
                    }, applicationLifetime.ApplicationStopping);
                }

                return _activeTenants;
            };

            var retVal = await _cache.GetOrAddAsync("tenants", getAllActiveTenantsFactory, DateTimeOffset.Now.AddMinutes(CacheExpiryMinutes)).ConfigureAwait(false);

            return retVal;
        }

        public virtual async Task InitializeTenantsAsync(CancellationToken cancellationToken = default)
        {
            await GetAllTenantsAsync(true).ConfigureAwait(false); ;
        }

        public virtual async Task<HangfireTenant> GetTenantByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var allTenants = await GetAllTenantsAsync(false, cancellationToken).ConfigureAwait(false);
            var tenant = allTenants.FirstOrDefault(t => t.Id == id.ToString());
            return tenant;
        }
    }
}
