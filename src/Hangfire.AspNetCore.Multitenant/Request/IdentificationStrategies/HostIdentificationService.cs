using Autofac.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public class HostIdentificationService : ITenantIdentificationStrategy
    {
        private readonly ILogger<ITenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        public HostIdentificationService(IHttpContextAccessor contextAccessor, IHostingEnvironment hostingEnvironment, ILogger<ITenantIdentificationStrategy> logger, IServiceProvider serviceProvider)
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

            //destination
            var host = httpContext.Request.Host.Value.Replace("www.","");
            var hostWithoutPort = host.Split(':')[0];
            var subdomain = hostWithoutPort.Contains(".");

            //ip restriction security
            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            Func<HangfireTenant, bool> exactMatchHostWithPortCondition = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Contains(host);
            Func<HangfireTenant, bool> exactMatchHostWithoutPortCondition = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Contains(hostWithoutPort);
            Func<HangfireTenant, bool> nonSubdomainCondition = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Any(h => h == "") && !subdomain;
            Func<HangfireTenant, bool> endWildcardCondition = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Any(h => h.EndsWith("*") && host.StartsWith(h.Replace("*", "")));
            Func<HangfireTenant, bool> startWildcardWithPortCondition = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Any(h => h.StartsWith("*") && host.EndsWith(h.Replace("*","")));
            Func<HangfireTenant, bool> startWildcardCondition = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Any(h => h.StartsWith("*") && hostWithoutPort.EndsWith(h.Replace("*", "")));

            IEnumerable<HangfireTenant> tenants;
            using(var scope = _serviceProvider.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<IHangfireTenantsStore>();
                 tenants = await store.GetAllTenantsAsync();
            }

            var exactMatchHostWithPort = tenants.Where(exactMatchHostWithPortCondition).ToList();
            var exactMatchHostWithoutPort = tenants.Where(exactMatchHostWithoutPortCondition).ToList();
            var nonSubdomain = tenants.Where(nonSubdomainCondition).ToList();

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
            else if (nonSubdomain.Count > 0)
            {
                tenant = nonSubdomain.First();
            }
            else if (endWildcard.Count > 0)
            {
                tenant = endWildcard.OrderByDescending(t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Max(hn => hn.Length)).First();
            }
            else if(startWildcardWithPort.Count > 0)
            {
                tenant = startWildcardWithPort.OrderByDescending(t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Max(hn => hn.Length)).First();
            }
            else if(startWildcard.Count > 0)
            {
                tenant = startWildcard.OrderByDescending(t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Max(hn => hn.Length)).First();
            }

            if (tenant != null)
            {
                if(tenant.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).IpAddressAllowed(ip))
                {
                    this._logger.LogInformation("Identified tenant: {tenant} from host: {host}", tenant.Id, host);
                    return tenant;
                }
            }

            _logger.LogWarning("Unable to identify tenant from host.");
            return null;
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
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
