using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public interface IHangfireTenantsStore
    {
        Task<IEnumerable<HangfireTenant>> GetAllTenantsAsync();
        Task<HangfireTenant> GetTenantByIdAsync(object id);
        Task InitializeTenantsAsync();
    }
}
