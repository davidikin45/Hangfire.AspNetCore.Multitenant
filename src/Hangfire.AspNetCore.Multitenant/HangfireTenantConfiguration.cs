using Autofac.AspNetCore.Extensions;
using Autofac.Multitenant;
using Hangfire.AspNetCore.Extensions;
using Hangfire.Common;
using Hangfire.Initialization;
using Hangfire.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant
{
    public abstract class HangfireTenantConfiguration : ITenantConfiguration
    {
        public abstract object TenantId { get; }

        public abstract IEnumerable<string> HostNames { get; }

        public abstract void ConfigureAppConfiguration(TenantBuilderContext context, IConfigurationBuilder builder);

        public virtual void ConfigureServices(TenantBuilderContext context, IServiceCollection services)
        {
            var hangfireServicesAdded = context.RootServiceProvider.GetService<IGlobalConfiguration>() != null;

            //configuration
            var configuration = new HangfireConfiguration();
            context.Configuration.Bind("Hangfire", configuration);
            ConfigureHangfire(context, configuration);
            services.AddSingleton(configuration);

            //dashboard options
            var dashboardOptions = context.RootServiceProvider.GetService<DashboardOptions>()?.Clone() ?? new DashboardOptions();
            ConfigureHangfireDashboard(context, dashboardOptions);
            services.AddSingleton(dashboardOptions);

            //background processing server
            IBackgroundProcessingServer processingServer = null;

            //Storage
            if (configuration.Enabled)
            {
                var storageDetails = HangfireJobStorage.GetJobStorage(configuration.ConnectionString, options => {
                    options.PrepareSchemaIfNecessary = false;
                    options.EnableHeavyMigrations = false;
                    options.EnableLongPolling = configuration.EnableLongPolling;
                    options.SchemaName = configuration.SchemaName;
                });

                JobStorage storage = storageDetails.JobStorage;
                configuration.ExistingConnection = storageDetails.ExistingConnection;

                services.AddSingleton(storage);
                services.AddHangfireServerServices();

                Func<IServiceProvider, Action<IBackgroundProcessingServer>> processingServerSetter = ((sp) => (x) => { processingServer = x; });
                services.AddSingleton(processingServerSetter);
                Func<IServiceProvider, IBackgroundProcessingServer> processingServerAccessor = ((sp) => processingServer);
                services.AddSingleton(processingServerAccessor);

                var backgroundServerOptions = context.RootServiceProvider.GetService<BackgroundJobServerOptions>()?.Clone() ?? new BackgroundJobServerOptions();

                backgroundServerOptions.ServerName = configuration.ServerName ?? backgroundServerOptions.ServerName;
                backgroundServerOptions.Activator = new MultiTenantJobActivator(context.RootServiceProvider.GetRequiredService<MultitenantContainer>(), context.TenantId);
                backgroundServerOptions.FilterProvider = context.RootServiceProvider.GetService<IJobFilterProvider>() ?? new JobFilterCollection();

                ConfigureHangfireServer(context, backgroundServerOptions);

                services.AddSingleton(backgroundServerOptions);
            }
            else
            {
                if (hangfireServicesAdded)
                {
                    services.AddSingleton((JobStorage)new NoopJobStorage());
                    services.AddHangfireServerServices();
                }
            }

            //background processes
            ConfigureBackgroundProcesses(context, services);
        }

        public abstract void ConfigureBackgroundProcesses(TenantBuilderContext context, IServiceCollection services);

        public abstract void ConfigureHangfireDashboard(TenantBuilderContext context, DashboardOptions options);

        public abstract void ConfigureHangfireServer(TenantBuilderContext context, BackgroundJobServerOptions options);

        public abstract void ConfigureHangfire(TenantBuilderContext context, HangfireConfiguration configuration);

        public virtual async Task InitializeDbAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var jobStorage = serviceProvider.GetService<JobStorage>();
            var newHangfireServer = jobStorage != null && jobStorage.GetType() != typeof(NoopJobStorage);
            if (newHangfireServer)
            {
                var configuration = serviceProvider.GetRequiredService<HangfireConfiguration>();

                //Initialize Hangfire Db
                if (!string.IsNullOrEmpty(configuration.ConnectionString))
                {
                    if (configuration.DbInitialiation == DbInitialiation.PrepareSchemaIfNecessary)
                    {
                        if (configuration.ExistingConnection == null)
                        {
                            HangfireJobStorage.GetJobStorage(configuration.ConnectionString, options => {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = false;
                                options.EnableLongPolling = false;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                        else
                        {
                            HangfireJobStorage.GetJobStorage(configuration.ExistingConnection, options => {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = false;
                                options.EnableLongPolling = false;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                    }
                    else if (configuration.DbInitialiation == DbInitialiation.PrepareSchemaIfNecessaryAndHeavyMigrations)
                    {
                        if (configuration.ExistingConnection == null)
                        {
                            HangfireJobStorage.GetJobStorage(configuration.ConnectionString, options =>
                            {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = true;
                                options.EnableLongPolling = false;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                        else
                        {
                            HangfireJobStorage.GetJobStorage(configuration.ExistingConnection, options =>
                            {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = true;
                                options.EnableLongPolling = false;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                    }
                    else if (configuration.DbInitialiation == DbInitialiation.EnsureTablesDeletedThenEnsureDbAndTablesCreated)
                    {
                        if (configuration.ExistingConnection == null)
                        {
                            await HangfireInitializer.EnsureTablesDeletedAsync(configuration.ConnectionString, configuration.SchemaName);
                            await HangfireInitializer.EnsureDbAndTablesCreatedAsync(configuration.ConnectionString, options =>
                            {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = true;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                        else
                        {
                            await HangfireInitializer.EnsureTablesDeletedAsync(configuration.ExistingConnection, configuration.SchemaName);
                            await HangfireInitializer.EnsureDbAndTablesCreatedAsync(configuration.ExistingConnection, options =>
                            {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = true;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                    }
                    else if (configuration.DbInitialiation == DbInitialiation.EnsureDbAndTablesCreated)
                    {
                        if (configuration.ExistingConnection == null)
                        {
                            await HangfireInitializer.EnsureDbAndTablesCreatedAsync(configuration.ConnectionString, options =>
                            {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = false;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                        else
                        {
                            await HangfireInitializer.EnsureDbAndTablesCreatedAsync(configuration.ExistingConnection, options =>
                            {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = false;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                    }
                    else if (configuration.DbInitialiation == DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations)
                    {
                        if (configuration.ExistingConnection == null)
                        {
                            await HangfireInitializer.EnsureDbAndTablesCreatedAsync(configuration.ConnectionString, options => {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = true;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                        else
                        {
                            await HangfireInitializer.EnsureDbAndTablesCreatedAsync(configuration.ExistingConnection, options => {
                                options.PrepareSchemaIfNecessary = true;
                                options.EnableHeavyMigrations = true;
                                options.SchemaName = configuration.SchemaName;
                            });
                        }
                    }
                }

                //Launch Server
                var applicationLifetime = serviceProvider.GetRequiredService<IApplicationLifetime>();
                var additionalProcesses = serviceProvider.GetServices<IBackgroundProcess>();
                var setter = serviceProvider.GetRequiredService<Action<IBackgroundProcessingServer>>();
                var serverDetails = HangfireLauncher.StartHangfireServer(serviceProvider.GetRequiredService<BackgroundJobServerOptions>(), jobStorage, new HangfireLauncherOptions() { ApplicationLifetime = applicationLifetime, AdditionalProcesses = additionalProcesses });
                setter(serverDetails.Server);
            }
        }

        public virtual Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            ConfigureHangfireJobs(recurringJobManager);
            return Task.CompletedTask;
        }

        public abstract void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager);

        //public async Task OnTenantUpdated(HangfireTenant updatedTenant, HangfireTenant oldTenant)
        //{
        //    //If connection string changes restart server.
        //    if (oldTenant.DefaultConfig.HangfireConnectionString != updatedTenant.DefaultConfig.HangfireConnectionString)
        //    {
        //        await OnTenantRemoved(updatedTenant);
        //        await OnTenantAdded(updatedTenant);
        //    }
        //}

        //public Task OnTenantRemoved(HangfireTenant tenant)
        //{
        //    var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
        //    using (var scope = new AutofacServiceProvider(multitenantContainer.GetTenantScope(tenant.Id).BeginLifetimeScope()))
        //    {
        //        var backgroundProcessingServer = scope.GetService<IBackgroundProcessingServer>();
        //        backgroundProcessingServer?.Dispose();
        //    }

        //    multitenantContainer.RemoveTenant(tenant.Id);

        //    _logger.LogInformation($"Tenant {tenant.Id} Removed");
        //    return Task.CompletedTask;
        //}
    }
}
