using Autofac.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class QueryStringIdentificationService : ITenantIdentificationStrategy
    {
        private readonly ILogger<ITenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        public QueryStringIdentificationService(IHttpContextAccessor contextAccessor, IHostingEnvironment hostingEnvironment, ILogger<ITenantIdentificationStrategy> logger, IServiceProvider serviceProvider)
        {
            _contextAccessor = contextAccessor;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<HangfireTenant> GetTenantAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return null;
            }

            //ip restriction security
            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            var tenantId = httpContext.Request.Query["TenantId"].ToString();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                HangfireTenant tenant;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var store = scope.ServiceProvider.GetRequiredService<IHangfireTenantsStore>();
                    tenant = await store.GetTenantByIdAsync(tenantId);
                }

                if (tenant != null)
                {
                    if (tenant.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).IpAddressAllowed(ip))
                    {
                        _logger.LogInformation("Identified tenant: {tenant} from query string", tenant.Id);
                        return tenant;
                    }
                }

                _logger.LogWarning("Unable to identify tenant from query string.");
            }

            return null;
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
            var httpContext = _contextAccessor.HttpContext;

            var tenant = GetTenantAsync(httpContext).GetAwaiter().GetResult();
            if (tenant != null)
            {
                tenantId = tenant.Id;
                return true;
            }

            tenantId = null;
            return false;
        }
    }
}
