using LazyCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public abstract class HangfireTenantsInMemoryCachingStore : IHangfireTenantsStore
    {
        private readonly IAppCache _cache;

        public virtual bool CachingEnabled { get; set; } = true;
        public virtual int CacheExpiryMinutes { get; set; } = 20;

        public HangfireTenantsInMemoryCachingStore()
        {
            _cache = new CachingService();
        }
        public abstract Task<IEnumerable<HangfireTenant>> GetAllActiveTenantsFromStoreAsync();

        private static List<HangfireTenant> _activeTenants = new List<HangfireTenant>();
        public virtual async Task<IEnumerable<HangfireTenant>> GetAllTenantsAsync()
        {
            Func<Task<IEnumerable<HangfireTenant>>> getAllActiveTenantsFactory = async () => {

                var previousTenantIds = _activeTenants?.Select(t => t.Id) ?? Enumerable.Empty<string>();
                var allTenants = (await GetAllActiveTenantsFromStoreAsync()).Where(t => t.Active);

                var activeTenantIds = allTenants?.Select(t => t.Id) ?? Enumerable.Empty<string>();

                var addedTenants = allTenants.Where(t => !previousTenantIds.Contains(t.Id));
                var removedTenants = _activeTenants?.Where(t => !activeTenantIds.Contains(t.Id)) ?? Enumerable.Empty<HangfireTenant>();

                foreach (var removedTenant in removedTenants)
                {
                    _activeTenants.RemoveAll(t => t.Id == removedTenant.Id);
                    await OnTenantRemoved(removedTenant);
                }

                foreach (var addedTenant in addedTenants)
                {
                    _activeTenants.Add(addedTenant);
                    await OnTenantAdded(addedTenant);
                }

                return _activeTenants;
            };

            var retVal = await _cache.GetOrAddAsync("tenants", getAllActiveTenantsFactory, DateTimeOffset.Now.AddMinutes(CacheExpiryMinutes));

            return retVal;
        }

        public virtual async Task InitializeTenantsAsync()
        {
            await GetAllTenantsAsync();
        }

        public virtual async Task<HangfireTenant> GetTenantByIdAsync(object id)
        {
            var allTenants = await GetAllTenantsAsync();
            var tenant = allTenants.FirstOrDefault(t => t.Id == id.ToString());
            return tenant;
        }

        public abstract Task OnTenantAdded(HangfireTenant newTenant);
        public abstract Task OnTenantRemoved(HangfireTenant removedTenant);
    }
}
