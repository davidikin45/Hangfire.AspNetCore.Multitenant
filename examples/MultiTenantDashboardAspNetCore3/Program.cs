using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Autofac.AspNetCore.Extensions;

namespace MultiTenantDashboardAspNetCore3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
             .UseAutofacMultitenant((context, options) =>
             {
                 options.ValidateOnBuild = false;
                 options.MapDefaultTenantToAllRootDomains(); //Comment if you dont wish for there to be a "" tenant
                 options.AddTenantsFromConfig(context.Configuration);
                 options.ConfigureTenants(builder =>
                 {
                     builder.MapToTenantIdSubDomain();
                 });
             })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                })
                .UseStartup<Startup>();
            });
    }
}
