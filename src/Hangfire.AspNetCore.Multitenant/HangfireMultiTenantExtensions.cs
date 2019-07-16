using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class HangfireMultiTenantExtensions
    {
        public static IApplicationBuilder UseHangfireDashboardMultiTenant(
            this IApplicationBuilder app,
            string pathMatch = "/hangfire",
            Func<HttpContext, Task<DashboardOptions>> options = null,
            Func<HttpContext, Task<JobStorage>> storage = null)
        {
            return app.Use(async (context, next) =>
            {
                var jobStorage = storage != null ? await storage(context) : null;
                var dashboardOptions = options != null ? await options(context) : null;

                var middleware = app.New();
                
                if(jobStorage != null)
                {
                    middleware.UseHangfireDashboard(pathMatch, dashboardOptions, jobStorage);
                }

                middleware.Run(async context2 => await next());

                await middleware.Build().Invoke(context);
                // Do logging or other work that doesn't write to the Response.
            });
        }
    }

}
