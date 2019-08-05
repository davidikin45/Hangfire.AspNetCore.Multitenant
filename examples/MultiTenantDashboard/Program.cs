using Autofac.AspNetCore.Extensions;
using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
                    var applicationLifetime = serviceProvider.GetRequiredService<IApplicationLifetime>();
                    var tenantStore = serviceProvider.GetRequiredService<IHangfireTenantsStore>();
                    await tenantStore.InitializeTenantsAsync(applicationLifetime.ApplicationStopping);
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
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                })
                .UseAutofacMultiTenant()
                .UseStartup<Startup>();
    }
}