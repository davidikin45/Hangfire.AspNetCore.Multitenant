using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace MultiTenantDashboard
{
    public class HangfireTenantsJson : HangfireTenantsJsonStore
    {
        public HangfireTenantsJson(ILogger<HangfireTenantsJson> logger, IServiceProvider serviceProvider, IHostingEnvironment hostingEnvironment, IOptions<HangfireTenantsJsonStoreOptions> options)
            :base(logger, serviceProvider, hostingEnvironment, options)
        {
            CacheExpiryMinutes = 1;
        }

        //Kestral is good for testing multitenant apps as localhost subdomains works without needing to modify hosts file. By default appsettings.json "AllowedHosts": "*".

        //-- If using IIS Express will need to update hosts file.
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