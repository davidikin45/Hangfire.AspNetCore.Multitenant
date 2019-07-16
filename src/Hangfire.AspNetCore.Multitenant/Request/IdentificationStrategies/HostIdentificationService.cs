using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class HostIdentificationService : IHangfireTenantIdentificationStrategy
    {
        private readonly ILogger<IHangfireTenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHangfireTenantsStore _store;

        public HostIdentificationService(IHangfireTenantsStore store, IHttpContextAccessor contextAccessor, ILogger<IHangfireTenantIdentificationStrategy> logger)
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

            //destination
            var host = httpContext.Request.Host.Value.Replace("www.","");
            var hostWithoutPort = host.Split(':')[0];

            //ip restriction security
            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            Func<HangfireTenant, bool> exactMatchHostWithPortCondition = t => t.HostNames.Contains(host);
            Func<HangfireTenant, bool> exactMatchHostWithoutPortCondition = t => t.HostNames.Contains(hostWithoutPort);
            Func<HangfireTenant, bool> endWildcardCondition = t => t.HostNames.Any(h => h.EndsWith("*") && host.StartsWith(h.Replace("*", "")));
            Func<HangfireTenant, bool> startWildcardWithPortCondition = t => t.HostNames.Any(h => h.StartsWith("*") && host.EndsWith(h.Replace("*","")));
            Func<HangfireTenant, bool> startWildcardCondition = t => t.HostNames.Any(h => h.StartsWith("*") && hostWithoutPort.EndsWith(h.Replace("*", "")));

            var tenants = await _store.GetAllTenantsAsync();

            var exactMatchHostWithPort = tenants.Where(exactMatchHostWithPortCondition).ToList();
            var exactMatchHostWithoutPort = tenants.Where(exactMatchHostWithoutPortCondition).ToList();
            var endWildcard = tenants.Where(endWildcardCondition).ToList();
            var startWildcardWithPort = tenants.Where(startWildcardWithPortCondition).ToList();
            var startWildcard = tenants.Where(startWildcardCondition).ToList();

            HangfireTenant tenant = null;
            if(exactMatchHostWithPort.Count > 0)
            {
                if(exactMatchHostWithPort.Count == 1)
                {
                    tenant = exactMatchHostWithPort.First();
                }
            }
            else if(exactMatchHostWithoutPort.Count() > 0)
            {
                if (exactMatchHostWithoutPort.Count == 1)
                {
                    tenant = exactMatchHostWithoutPort.First();
                }
            }
            else if (endWildcard.Count > 0)
            {
                tenant = endWildcard.OrderByDescending(t => t.HostNames.Max(hn => hn.Length)).First();
            }
            else if(startWildcardWithPort.Count > 0)
            {
                tenant = startWildcardWithPort.OrderByDescending(t => t.HostNames.Max(hn => hn.Length)).First();
            }
            else if(startWildcard.Count > 0)
            {
                tenant = startWildcard.OrderByDescending(t => t.HostNames.Max(hn => hn.Length)).First();
            }

            if (tenant != null)
            {
                if(tenant.IpAddressAllowed(ip))
                {
                    this._logger.LogInformation("Identified tenant: {tenant} from host: {host}", tenant.Id, host);
                    TenantId = tenant.Id;
                    return tenant;
                }
            }

            TenantId = null;
            _logger.LogWarning("Unable to identify tenant from host.");
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
            if(tenant != null)
            {
                tenantId = tenant.Id;
                return true;
            }

            tenantId = null;
            return false;
        }
    }
}
