using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.Common;
using Hangfire.Initialization;
using Hangfire.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantDashboard
{
    public class HangfireTenantsStore : HangfireTenantsInMemoryStore
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireTenantsStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            CacheExpiryMinutes = 1;
        }

        public override async Task InitializeTenantsAsync()
        {
            //Server=(localdb)\\mssqllocaldb;Database=HangfireDatabase;Trusted_Connection=True;MultipleActiveResultSets=true; will use an SQLServer database.
            //"" will start a new hangfire server in memory.
            //Data Source=tenant1.db; will start a new SQLite hangfire server.
            //Data Source=:memory:; will start a new SQLite InMemory hangfire server.
            //null will mean tenant does not get a hangfire server.

            var sharedConnectionString = "Server=(localdb)\\mssqllocaldb;Database=HangfireMultitenant;Trusted_Connection=True;MultipleActiveResultSets=true;";
            //var sharedConnectionString = "Data Source=:memory:;";
            //var sharedConnectionString = "";

            Tenants = new List<HangfireTenant>(){
                new HangfireTenant(){ Id ="tenant0", HostNames = new string []{ "" }, DbInitialiation = DbInitialiation.EnsureTablesDeletedThenEnsureDbAndTablesCreated, HangfireConnectionString = sharedConnectionString, HangfireSchemaName ="tenant0"}
                .AddEnvionment("production", c => {c.HangfireConnectionString = "Data Source=tenant0.db;"; c.DbInitialiation = DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations; })
                };

            int tenantCount = 20;
            for (int i = 1; i <= tenantCount; i++)
            {
                Tenants.Add(new HangfireTenant() { Id = $"tenant{i}", HostNames = new string[] { $"tenant{i}.*" }, DbInitialiation = DbInitialiation.EnsureTablesDeletedThenEnsureDbAndTablesCreated, HangfireConnectionString = sharedConnectionString, HangfireSchemaName = $"tenant{i}" }
                .AddEnvionment("production", c => { c.HangfireConnectionString = $"Data Source=tenant{i}.db;"; c.DbInitialiation = DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations; }));
            }

            await base.InitializeTenantsAsync();
        }

        public override async Task OnTenantAdded(HangfireTenant tenant)
        {
            var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
            var services = new AutofacServiceProvider(multitenantContainer);
            var hangfireServicesAdded = _serviceProvider.GetService<IGlobalConfiguration>() != null;

            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            var environment = _serviceProvider.GetRequiredService<IHostingEnvironment>();
            var applicationLifetime = _serviceProvider.GetRequiredService<IApplicationLifetime>();

            var tenantConfigurations = services.GetServices<IHangfireTenantConfiguration>();

            var tenantInitializer = tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == tenant.Id);

            var actionBuilder = new ConfigurationActionBuilder();
            var tenantServices = new ServiceCollection();

            var dashboardOptions = services.GetService<DashboardOptions>()?.Clone() ?? new DashboardOptions();

            var environmentConfig = tenant.GetEnvironmentConfig(environment.EnvironmentName);

            if (tenantInitializer != null)
            {
                tenantInitializer.ConfigureServices(tenant, tenantServices, configuration, environment);
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

                var backgroundServerOptions = services.GetService<BackgroundJobServerOptions>()?.Clone() ?? new BackgroundJobServerOptions() { ServerName = environmentConfig.HangfireServerName, Queues = new string[] { environmentConfig.HangfireServerName, "default" } };
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
                    await tenantInitializer.InitializeAsync(tenant, scope, configuration, environment);
                }

                if (newHangfireServer)
                {
                    //Initialize Hangfire Db
                    if (!string.IsNullOrEmpty(environmentConfig.HangfireConnectionString))
                    {
                        if (environmentConfig.DbInitialiation == DbInitialiation.PrepareSchemaIfNecessary)
                        {
                            if(existingConnection == null)
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
                        else if(environmentConfig.DbInitialiation == DbInitialiation.PrepareSchemaIfNecessaryAndHeavyMigrations)
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
                            if(existingConnection == null)
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
                        tenantInitializer.ConfigureHangfireJobs(scope.GetRequiredService<IRecurringJobManager>(), configuration, environment);
                    }

                    var additionalProcesses = scope.GetServices<IBackgroundProcess>();

                    var jobStorage = scope.GetService<JobStorage>();
                    var serverDetails = HangfireLauncher.StartHangfireServer(scope.GetRequiredService<BackgroundJobServerOptions>(), jobStorage, new HangfireLauncherOptions() { ApplicationLifetime = applicationLifetime, AdditionalProcesses = additionalProcesses });
                    processingServer = serverDetails.Server;
                }
            }
        }

        public override Task OnTenantRemoved(HangfireTenant tenant)
        {
            var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
            using (var scope = new AutofacServiceProvider(multitenantContainer.GetTenantScope(tenant.Id).BeginLifetimeScope()))
            {
                var backgroundProcessingServer = scope.GetService<IBackgroundProcessingServer>();
                backgroundProcessingServer?.Dispose();
            }

            multitenantContainer.RemoveTenant(tenant.Id);
            return Task.CompletedTask;
        }

        //C:\WINDOWS\system32\drivers\etc\hosts
        //127.0.0.1 tenant1.localhost
        //127.0.0.1 tenant2.localhost
        //127.0.0.1 tenant3.localhost
        //127.0.0.1 tenant4.localhost
        //127.0.0.1 tenant5.localhost
        //127.0.0.1 tenant6.localhost
        //127.0.0.1 tenant7.localhost
        //127.0.0.1 tenant8.localhost
        //127.0.0.1 tenant9.localhost
        //127.0.0.1 tenant10.localhost
    }
}