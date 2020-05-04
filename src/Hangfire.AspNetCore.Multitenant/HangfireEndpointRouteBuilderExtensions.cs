using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class HangfireEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHangfireMultiTenantDashboard(this IEndpointRouteBuilder endpoints,
            string route = "/hangfire",
            Func<HttpContext, Task<DashboardOptions>> options = null,
            Func<HttpContext, Task<JobStorage>> storage = null)
        {
            var requestHandler = endpoints.CreateApplicationBuilder().UseHangfireDashboardMultiTenant(route, options, storage).Build();
            return endpoints.Map(route + "/{**path}", requestHandler);
        }
    }
}
