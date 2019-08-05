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
    public class SourceIPIdentificationService : ITenantIdentificationStrategy
    {
        private readonly ILogger<ITenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IServiceProvider _serviceProvider;

        public SourceIPIdentificationService(IHttpContextAccessor contextAccessor, IHostingEnvironment hostingEnvironment, ILogger<ITenantIdentificationStrategy> logger, IServiceProvider serviceProvider)
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

            //origin
            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            Func<HangfireTenant, bool> whereClause = t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses != null && t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).HostNames.Count() == 0
            && (
              t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses.Where(i => !i.Contains("*") || i.EndsWith("*")).Any(i => ip.StartsWith(i.Replace("*", "")))
             || t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses.Where(i => i.StartsWith("*")).Any(i => ip.EndsWith(i.Replace("*", "")))
             );

            IEnumerable<HangfireTenant> tenants;
            using (var scope = _serviceProvider.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<IHangfireTenantsStore>();
                tenants = await store.GetAllTenantsAsync();
            }

            var filteredTenants = tenants.Where(whereClause).ToList();

            var tenant = filteredTenants.OrderByDescending(t => t.GetEnvironmentConfig(_hostingEnvironment.EnvironmentName).RequestIpAddresses.Max(hn => hn.Length)).FirstOrDefault();

            if (tenant != null)
            {
                _logger.LogInformation("Identified tenant: {tenant} from ip: {ip}", tenant.Id, ip);
                return tenant;
            }

            _logger.LogWarning("Unable to identify tenant from ip address.");
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
