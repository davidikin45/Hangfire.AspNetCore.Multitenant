using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Middleware
{
    public class Tenant404Middleware<TTenant>
    {
        private readonly RequestDelegate _next;

        public Tenant404Middleware(RequestDelegate next)
        {
            this._next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Items.ContainsKey("tenantId") == false)
            {
                var service = context.RequestServices.GetRequiredService<ITenantIdentificationStrategy>();
                var tenant = service.TryIdentifyTenant(out var tenantId);
                if (tenantId != null)
                {
                    //var configuration = context.RequestServices.GetService<IConfiguration>();
                    //var environment = context.RequestServices.GetService<IHostingEnvironment>();
                    //var providers = (configuration as ConfigurationRoot).Providers as List<IConfigurationProvider>;

                    //var tenatProviders = providers.OfType<TenantJsonConfigurationProvider>().ToList();
                    //if (tenatProviders.Count == 0)
                    //{
                    //    var tenantProviders = (TenantConfig.BuildTenantConfiguration(environment, tenant.Id) as ConfigurationRoot).Providers as List<IConfigurationProvider>;
                    //    //providers.Insert(2, tenantProviders[0]);
                    //    //providers.Insert(4, tenantProviders[1]);
                    //}
                    //else
                    //{
                    //    //var tenantProviders = (TenantConfig.BuildTenantConfiguration(environment, tenant.Id) as ConfigurationRoot).Providers as List<IConfigurationProvider>;
                    //    //providers[2] = tenantProviders[0];
                    //    //providers[4] = tenantProviders[1];
                    //}
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }
                context.Items["tenantId"] = tenant;
            }

            return this._next(context);
        }
    }
}
