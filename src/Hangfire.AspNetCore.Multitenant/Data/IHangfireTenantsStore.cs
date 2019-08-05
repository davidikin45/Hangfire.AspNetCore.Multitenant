using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public interface IHangfireTenantsStore
    {
        Task<IEnumerable<HangfireTenant>> GetAllTenantsAsync(bool waitForInitialization = false, CancellationToken cancellationToken = default);
        Task<HangfireTenant> GetTenantByIdAsync(object id, CancellationToken cancellationToken = default);
        Task InitializeTenantsAsync(CancellationToken cancellationToken = default);
    }
}
