using Autofac;
using Autofac.AspNetCore.Extensions;
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
                   var tenantStore = serviceProvider.GetRequiredService<IHangfireTenantsStore>();
                   await tenantStore.InitializeTenantsAsync();
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