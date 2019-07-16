using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class QueryStringIdentificationService : IHangfireTenantIdentificationStrategy
    {
        private readonly ILogger<IHangfireTenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHangfireTenantsStore _store;

        public QueryStringIdentificationService(IHangfireTenantsStore store, IHttpContextAccessor contextAccessor, ILogger<IHangfireTenantIdentificationStrategy> logger)
        {
            _store = store;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        public object TenantId { get; set; }

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
                var tenant = await _store.GetTenantByIdAsync(tenantId);
                if (tenant != null)
                {
                    if (tenant.IpAddressAllowed(ip))
                    {
                        TenantId = tenant.Id;
                        _logger.LogInformation("Identified tenant: {tenant} from query string", tenant.Id);
                        return tenant;
                    }
                }
            }

            TenantId = null;
            _logger.LogWarning("Unable to identify tenant from query string.");
            return null;
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
            if (TenantId != null)
            {
                tenantId = TenantId;
                return true;
            }

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
