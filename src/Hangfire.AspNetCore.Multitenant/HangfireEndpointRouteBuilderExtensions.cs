using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
#if NETCOREAPP3_0
using Microsoft.AspNetCore.Routing;
#endif
using System;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class HangfireEndpointRouteBuilderExtensions
    {
#if NETCOREAPP3_0
        public static IEndpointConventionBuilder MapHangfireMultiTenantDashboard(this IEndpointRouteBuilder endpoints, 
            string route = "/hangfire", 
            Func<HttpContext, Task<DashboardOptions>> options = null,
            Func<HttpContext, Task<JobStorage>> storage = null)
        {
            var requestHandler = endpoints.CreateApplicationBuilder().UseHangfireDashboardMultiTenant(route, options, storage).Build();
            return endpoints.Map(route + "/{**path}", requestHandler);
        }
#endif
    }
}
