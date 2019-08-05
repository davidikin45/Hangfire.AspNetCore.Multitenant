using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Hangfire;
using Hangfire.AspNetCore.Extensions;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.Common;
using Hangfire.Initialization;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantDashboard
{
    public class HangfireTenantSetup : IHangfireTenantSetup
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        public HangfireTenantSetup(IServiceProvider serviceProvider, ILogger<HangfireTenantSetup> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task OnTenantAdded(HangfireTenant tenant)
        {
            var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
            var services = new AutofacServiceProvider(multitenantContainer);
            var hangfireServicesAdded = services.GetService<IGlobalConfiguration>() != null;

            var configuration = services.GetRequiredService<IConfiguration>();
            var environment = services.GetRequiredService<IHostingEnvironment>();
            var applicationLifetime = services.GetRequiredService<IApplicationLifetime>();

            var tenantConfigurations = services.GetServices<IHangfireTenantConfiguration>();

            var tenantInitializer = tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == tenant.Id);

            var actionBuilder = new ConfigurationActionBuilder();
            var tenantServices = new ServiceCollection();

            var dashboardOptions = services.GetService<DashboardOptions>()?.Clone() ?? new DashboardOptions();

            var environmentConfig = tenant.GetEnvironmentConfig(environment.EnvironmentName);

            if (tenantInitializer != null)
            {
                tenantInitializer.ConfigureServices(tenant, tenantServices);
                tenantInitializer.ConfigureHangfireDashboard(dashboardOptions);
            }

            tenantServices.AddSingleton(dashboardOptions);

            IBackgroundProcessingServer processingServer = null;
            bool newHangfireServer = false;
            DbConnection existingConnection = null;
            if (environmentConfig.HangfireConnectionString != null)
            {
                newHangfireServer = true;
                var storageDetails = HangfireJobStorage.GetJobStorage(environmentConfig.HangfireConnectionString, options => {
                    options.PrepareSchemaIfNecessary = false;
                    options.EnableHeavyMigrations = false;
                    options.EnableLongPolling = environmentConfig.HangfireEnableLongPolling.HasValue && environmentConfig.HangfireEnableLongPolling.Value;
                    options.SchemaName = environmentConfig.HangfireSchemaName;
                });

                JobStorage storage = storageDetails.JobStorage;
                existingConnection = storageDetails.ExistingConnection;

                tenantServices.AddSingleton(storage);
                tenantServices.AddHangfireServerServices();

                Func<IServiceProvider, IBackgroundProcessingServer> processingServerAccessor = ((sp) => processingServer);
                tenantServices.AddSingleton(processingServerAccessor);

                var backgroundServerOptions = services.GetService<BackgroundJobServerOptions>()?.Clone() ?? new BackgroundJobServerOptions();

                backgroundServerOptions.ServerName = environmentConfig.HangfireServerName ?? backgroundServerOptions.ServerName;
                backgroundServerOptions.Activator = new MultiTenantJobActivator(multitenantContainer, tenant.Id);
                backgroundServerOptions.FilterProvider = services.GetService<IJobFilterProvider>() ?? new JobFilterCollection();

                if (tenantInitializer != null)
                {
                    tenantInitializer.ConfigureHangfireServer(backgroundServerOptions);
                }

                tenantServices.AddSingleton(backgroundServerOptions);
            }
            else
            {
                if (hangfireServicesAdded)
                {
                    tenantServices.AddSingleton(new NoopJobStorage());
                    tenantServices.AddHangfireServerServices();
                }
            }

            actionBuilder.Add(b => b.Populate(tenantServices));
            multitenantContainer.ConfigureTenant(tenant.Id, actionBuilder.Build());

            using (var scope = new AutofacServiceProvider(multitenantContainer.GetTenantScope(tenant.Id).BeginLifetimeScope()))
            {
                if (tenantInitializer != null)
                {
                    await tenantInitializer.InitializeAsync(tenant, scope);
                }

                if (newHangfireServer)
                {
                    //Initialize Hangfire Db
                    if (!string.IsNullOrEmpty(environmentConfig.HangfireConnectionString))
                    {
                        if (environmentConfig.DbInitialiation == DbInitialiation.PrepareSchemaIfNecessary)
                        {
                            if (existingConnection == null)
                            {
                                HangfireJobStorage.GetJobStorage(environmentConfig.HangfireConnectionString, options => {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = false;
                                    options.EnableLongPolling = false;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                            else
                            {
                                HangfireJobStorage.GetJobStorage(existingConnection, options => {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = false;
                                    options.EnableLongPolling = false;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                        }
                        else if (environmentConfig.DbInitialiation == DbInitialiation.PrepareSchemaIfNecessaryAndHeavyMigrations)
                        {
                            if (existingConnection == null)
                            {
                                HangfireJobStorage.GetJobStorage(environmentConfig.HangfireConnectionString, options =>
                                {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = true;
                                    options.EnableLongPolling = false;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                            else
                            {
                                HangfireJobStorage.GetJobStorage(existingConnection, options =>
                                {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = true;
                                    options.EnableLongPolling = false;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                        }
                        else if (environmentConfig.DbInitialiation == DbInitialiation.EnsureTablesDeletedThenEnsureDbAndTablesCreated)
                        {
                            if (existingConnection == null)
                            {
                                await HangfireInitializer.EnsureTablesDeletedAsync(environmentConfig.HangfireConnectionString, environmentConfig.HangfireSchemaName);
                                await HangfireInitializer.EnsureDbAndTablesCreatedAsync(environmentConfig.HangfireConnectionString, options =>
                                {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = true;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                            else
                            {
                                await HangfireInitializer.EnsureTablesDeletedAsync(existingConnection, environmentConfig.HangfireSchemaName);
                                await HangfireInitializer.EnsureDbAndTablesCreatedAsync(existingConnection, options =>
                                {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = true;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                        }
                        else if (environmentConfig.DbInitialiation == DbInitialiation.EnsureDbAndTablesCreated)
                        {
                            if (existingConnection == null)
                            {
                                await HangfireInitializer.EnsureDbAndTablesCreatedAsync(environmentConfig.HangfireConnectionString, options =>
                                {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = false;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                            else
                            {
                                await HangfireInitializer.EnsureDbAndTablesCreatedAsync(existingConnection, options =>
                                {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = false;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                        }
                        else if (environmentConfig.DbInitialiation == DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations)
                        {
                            if (existingConnection == null)
                            {
                                await HangfireInitializer.EnsureDbAndTablesCreatedAsync(environmentConfig.HangfireConnectionString, options => {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = true;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                            else
                            {
                                await HangfireInitializer.EnsureDbAndTablesCreatedAsync(existingConnection, options => {
                                    options.PrepareSchemaIfNecessary = true;
                                    options.EnableHeavyMigrations = true;
                                    options.SchemaName = environmentConfig.HangfireSchemaName;
                                });
                            }
                        }
                    }

                    if (tenantInitializer != null)
                    {
                        tenantInitializer.ConfigureHangfireJobs(scope.GetRequiredService<IRecurringJobManager>());
                    }

                    var additionalProcesses = scope.GetServices<IBackgroundProcess>();

                    var jobStorage = scope.GetService<JobStorage>();
                    var serverDetails = HangfireLauncher.StartHangfireServer(scope.GetRequiredService<BackgroundJobServerOptions>(), jobStorage, new HangfireLauncherOptions() { ApplicationLifetime = applicationLifetime, AdditionalProcesses = additionalProcesses });
                    processingServer = serverDetails.Server;
                }
            }

            _logger.LogInformation($"Tenant {tenant.Id} Initialized");
        }
        public async Task OnTenantUpdated(HangfireTenant updatedTenant, HangfireTenant oldTenant)
        {
            //If connection string changes restart server.
            if (oldTenant.DefaultConfig.HangfireConnectionString != updatedTenant.DefaultConfig.HangfireConnectionString)
            {
                await OnTenantRemoved(updatedTenant);
                await OnTenantAdded(updatedTenant);
            }
        }

        public Task OnTenantRemoved(HangfireTenant tenant)
        {
            var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
            using (var scope = new AutofacServiceProvider(multitenantContainer.GetTenantScope(tenant.Id).BeginLifetimeScope()))
            {
                var backgroundProcessingServer = scope.GetService<IBackgroundProcessingServer>();
                backgroundProcessingServer?.Dispose();
            }

            multitenantContainer.RemoveTenant(tenant.Id);

            _logger.LogInformation($"Tenant {tenant.Id} Removed");
            return Task.CompletedTask;
        }
    }
}
