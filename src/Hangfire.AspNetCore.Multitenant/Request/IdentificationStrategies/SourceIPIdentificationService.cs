using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class SourceIPIdentificationService : IHangfireTenantIdentificationStrategy
    {
        private readonly ILogger<IHangfireTenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IHangfireTenantsStore _store;

        public SourceIPIdentificationService(IHangfireTenantsStore store, IHttpContextAccessor contextAccessor, IHostingEnvironment hostingEnvironment, ILogger<IHangfireTenantIdentificationStrategy> logger)
        {
            _store = store;
            _contextAccessor = contextAccessor;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        public object TenantId { get; set; }

        public async Task<HangfireTenant> GetTenantAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return null;
            }

            //origin
            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            Func<HangfireTenant, bool> whereClause = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses != null && t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Count() == 0
            && (
              t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses.Where(i => !i.Contains("*") || i.EndsWith("*")).Any(i => ip.StartsWith(i.Replace("*", "")))
             || t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses.Where(i => i.StartsWith("*")).Any(i => ip.EndsWith(i.Replace("*", "")))
             );

            var tenants = await _store.GetAllTenantsAsync();

            var filteredTenants = tenants.Where(whereClause).ToList();

            var tenant = filteredTenants.OrderByDescending(t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses.Max(hn => hn.Length)).FirstOrDefault();

            if (tenant != null)
            {
                TenantId = tenant.Id;
                _logger.LogInformation("Identified tenant: {tenant} from ip: {ip}", tenant.Id, ip);
                return tenant;
            }

            TenantId = null;
            _logger.LogWarning("Unable to identify tenant from ip address.");
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
