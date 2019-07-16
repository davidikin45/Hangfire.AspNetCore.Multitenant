using Autofac;
using Autofac.Multitenant;
using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantDashboard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var webHost = CreateWebHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                try
                {
                    var multitenantContainer = serviceProvider.GetRequiredService<MultitenantContainer>();
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var environment = serviceProvider.GetRequiredService<IHostingEnvironment>();
                    var applicationLifetime = serviceProvider.GetRequiredService<IApplicationLifetime>();

                    var tenantsStore = serviceProvider.GetRequiredService<IHangfireTenantsStore>();
                    var tenantConfigurations = serviceProvider.GetServices<IHangfireTenantConfiguration>();

                    foreach (var tenant in (await tenantsStore.GetAllTenantsAsync()).ToList())
                    {
                        var actionBuilder = new ConfigurationActionBuilder();

                        var tenantInitializer = tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == tenant.Id);

                        if (tenantInitializer != null)
                        {
                            tenantInitializer.ConfigureServices(actionBuilder, configuration, environment);
                        }

                        if (tenant.HangfireConnectionString != null)
                        {
                            var serverDetails = HangfireMultiTenantLauncher.StartHangfireServer(tenant.Id, "web-background", tenant.HangfireConnectionString, multitenantContainer, options => { options.ApplicationLifetime = applicationLifetime; options.AdditionalProcesses = tenant.AdditionalProcesses; });

                            tenant.Storage = serverDetails.Storage;

                            if (tenantInitializer != null)
                            {
                                tenantInitializer.ConfigureHangfireJobs(serverDetails.recurringJobManager, configuration, environment);
                            }

                            actionBuilder.Add(b => b.RegisterInstance(serverDetails.recurringJobManager).As<IRecurringJobManager>().SingleInstance());
                            actionBuilder.Add(b => b.RegisterInstance(serverDetails.backgroundJobClient).As<IBackgroundJobClient>().SingleInstance());
                        }
                        else
                        {
                            actionBuilder.Add(b => b.RegisterInstance<IRecurringJobManager>(null).As<IRecurringJobManager>().SingleInstance());
                            actionBuilder.Add(b => b.RegisterInstance<IBackgroundJobClient>(null).As<IBackgroundJobClient>().SingleInstance());
                        }

                        multitenantContainer.ConfigureTenant(tenant.Id, actionBuilder.Build());
                    }
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserInitialisation");
                    logger.LogError(ex, "Failed to Initialize");
                }
            }
            
            await webHost.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseAutofacMultiTenant()
                .UseStartup<Startup>();
    }
}