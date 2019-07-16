using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.AspNetCore.Multitenant.Request
{
    public class HangfireTenantRequestIdentificationBuilder
    {
        internal readonly IServiceCollection _services;

        internal HangfireTenantRequestIdentificationBuilder(IServiceCollection services)
        {
            this._services = services;
        }
    }
}
