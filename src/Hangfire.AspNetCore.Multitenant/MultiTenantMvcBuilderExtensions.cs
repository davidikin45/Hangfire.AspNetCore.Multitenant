using Hangfire.AspNetCore.Multitenant.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class MultiTenantMvcBuilderExtensions
    {
        /// <summary>
        /// Adds the hangfire tenant view location expander to the application. {tenantId}/view.cshtml
        /// </summary>
        public static IMvcBuilder AddHangfireTenantViewLocations(this IMvcBuilder builder)
        {
            builder.Services.Configure<RazorViewEngineOptions>(options =>
            {
                if (!(options.ViewLocationExpanders.FirstOrDefault() is TenantViewLocationExpander<HangfireTenant>))
                {
                    options.ViewLocationExpanders.Insert(0, new TenantViewLocationExpander<HangfireTenant>());
                }
            });

            return builder;
        }
    }
}
