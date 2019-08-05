using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Hangfire.AspNetCore.Multitenant.Middleware
{
    //Addition to AppStartup.Configure for configuring Request Pipeline
    //https://andrewlock.net/exploring-istartupfilter-in-asp-net-core/
    public class HangfireTenant404StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                //Adds the TenantMiddleware before ALL other middleware configured in Startup Configure.
                builder.UseMiddleware<Tenant404Middleware<HangfireTenant>>();
                next(builder);
            };
        }
    }
}

