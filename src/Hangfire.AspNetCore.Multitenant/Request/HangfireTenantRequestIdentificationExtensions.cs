using Autofac.Multitenant;
using Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Hangfire.AspNetCore.Multitenant.Request
{
    public static class HangfireTenantRequestIdentificationBuilderExtensions
    {
        public static IServiceCollection DynamicTenant(this HangfireTenantRequestIdentificationBuilder identification, Func<HttpContext, HangfireTenant> currentTenant, Func<IEnumerable<HangfireTenant>> allTenants)
        {
            identification._services.AddScoped<IHangfireTenantIdentificationStrategy>(sp => new DynamicTenantIdentificationService(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<IHangfireTenantIdentificationStrategy>>(), currentTenant, allTenants));
            return identification._services.AddScoped<ITenantIdentificationStrategy>(sp => sp.GetRequiredService<IHangfireTenantIdentificationStrategy>());
        }

        public static IServiceCollection TenantFromHostQueryStringSourceIP(this HangfireTenantRequestIdentificationBuilder identification)
        {
            identification._services.AddScoped<IHangfireTenantIdentificationStrategy, TenantHostQueryStringRequestIpIdentificationService>();
            return identification._services.AddScoped<ITenantIdentificationStrategy>(sp => sp.GetRequiredService<IHangfireTenantIdentificationStrategy>());
        }

        public static IServiceCollection TenantFromHost(this HangfireTenantRequestIdentificationBuilder identification)
        {
            identification._services.AddScoped<IHangfireTenantIdentificationStrategy, HostIdentificationService>();
            return identification._services.AddScoped<ITenantIdentificationStrategy>(sp => sp.GetRequiredService<IHangfireTenantIdentificationStrategy>());
        }

        public static IServiceCollection TenantFromQueryString(this HangfireTenantRequestIdentificationBuilder identification)
        {
            identification._services.AddScoped<IHangfireTenantIdentificationStrategy, QueryStringIdentificationService>();
            return identification._services.AddScoped<ITenantIdentificationStrategy>(sp => sp.GetRequiredService<IHangfireTenantIdentificationStrategy>());
        }

        public static IServiceCollection TenantFromSourceIP(this HangfireTenantRequestIdentificationBuilder identification)
        {
            identification._services.AddScoped<IHangfireTenantIdentificationStrategy, SourceIPIdentificationService>();
            return identification._services.AddScoped<ITenantIdentificationStrategy>(sp => sp.GetRequiredService<IHangfireTenantIdentificationStrategy>());
        }
    }
}
