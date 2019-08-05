using Autofac.Multitenant;
using Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Hangfire.AspNetCore.Multitenant.Request
{
    //ITenantIdentificationStrategy must be singleton!
    public static class HangfireTenantRequestIdentificationBuilderExtensions
    {
        public static IServiceCollection DynamicTenant(this HangfireTenantRequestIdentificationBuilder identification, Func<IHostingEnvironment, HttpContext, HangfireTenant> currentTenant, Func<IHostingEnvironment, IEnumerable<HangfireTenant>> allTenants)
        {
           return identification._services.AddSingleton<ITenantIdentificationStrategy>(sp => new DynamicTenantIdentificationService(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<IHostingEnvironment>(), sp.GetRequiredService<ILogger<ITenantIdentificationStrategy>>(), currentTenant, allTenants));
        }

        public static IServiceCollection TenantFromHostQueryStringSourceIP(this HangfireTenantRequestIdentificationBuilder identification)
        {
           return identification._services.AddSingleton<ITenantIdentificationStrategy, TenantHostQueryStringRequestIpIdentificationService>();
        }

        public static IServiceCollection TenantFromHost(this HangfireTenantRequestIdentificationBuilder identification)
        {
            return identification._services.AddSingleton<ITenantIdentificationStrategy, HostIdentificationService>();
        }

        public static IServiceCollection TenantFromQueryString(this HangfireTenantRequestIdentificationBuilder identification)
        {
            return identification._services.AddSingleton<ITenantIdentificationStrategy, QueryStringIdentificationService>();
        }

        public static IServiceCollection TenantFromSourceIP(this HangfireTenantRequestIdentificationBuilder identification)
        {
            return identification._services.AddSingleton<ITenantIdentificationStrategy, SourceIPIdentificationService>();
        }
    }
}
