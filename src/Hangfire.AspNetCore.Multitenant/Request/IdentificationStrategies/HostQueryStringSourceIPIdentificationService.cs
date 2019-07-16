using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class TenantHostQueryStringRequestIpIdentificationService : IHangfireTenantIdentificationStrategy
    {
        private readonly ILogger<IHangfireTenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHangfireTenantsStore _store;

        public TenantHostQueryStringRequestIpIdentificationService(IHangfireTenantsStore store, IHttpContextAccessor contextAccessor, ILogger<IHangfireTenantIdentificationStrategy> logger)
        {
            _store = store;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        public object TenantId { get; set; }

        public async Task<HangfireTenant> GetTenantAsync(HttpContext httpContext)
        {
            var hostIdentificationService = new HostIdentificationService(_store, _contextAccessor, _logger);
            var queryStringIdentificationService = new QueryStringIdentificationService(_store, _contextAccessor, _logger);
            var requestIpIdentificationService = new SourceIPIdentificationService(_store, _contextAccessor, _logger);


            var tenant = await queryStringIdentificationService.GetTenantAsync(httpContext);
            if (tenant != null)
            {
                return tenant;
            }

            //destination
            tenant = await hostIdentificationService.GetTenantAsync(httpContext);
            if (tenant != null)
            {
                return tenant;
            }

            //origin
            tenant = await requestIpIdentificationService.GetTenantAsync(httpContext);

            return tenant;
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
            if(TenantId != null)
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
