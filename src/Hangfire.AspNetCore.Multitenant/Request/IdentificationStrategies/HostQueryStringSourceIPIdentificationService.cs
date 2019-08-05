using Autofac.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class TenantHostQueryStringRequestIpIdentificationService : ITenantIdentificationStrategy
    {
        private readonly ILogger<ITenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        public TenantHostQueryStringRequestIpIdentificationService(IHttpContextAccessor contextAccessor, IHostingEnvironment hostingEnvironment, ILogger<ITenantIdentificationStrategy> logger, IServiceProvider serviceProvider)
        {
            _contextAccessor = contextAccessor;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<HangfireTenant> GetTenantAsync(HttpContext httpContext)
        {

            var hostIdentificationService = new HostIdentificationService(_contextAccessor, _hostingEnvironment, _logger, _serviceProvider);
            var queryStringIdentificationService = new QueryStringIdentificationService(_contextAccessor, _hostingEnvironment, _logger, _serviceProvider);
            var requestIpIdentificationService = new SourceIPIdentificationService(_contextAccessor, _hostingEnvironment, _logger, _serviceProvider);

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
