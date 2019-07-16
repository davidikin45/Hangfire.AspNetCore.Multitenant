using Autofac.Multitenant;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public interface IHangfireTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
          object TenantId { get; set; }
    }
}
