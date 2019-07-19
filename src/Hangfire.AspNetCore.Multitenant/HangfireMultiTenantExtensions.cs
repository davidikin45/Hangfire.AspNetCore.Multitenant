using Hangfire.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
                options = options ?? ((context2) => Task.FromResult(context2.RequestServices.GetService<DashboardOptions>()));
                storage = storage ?? ((context2) => Task.FromResult(context2.RequestServices.GetService<JobStorage>()));

                var jobStorage = await storage(context);
                var dashboardOptions = await options(context);

                var middleware = app.New();
                
                if(jobStorage != null && jobStorage.GetType() != typeof(NoopJobStorage))
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
