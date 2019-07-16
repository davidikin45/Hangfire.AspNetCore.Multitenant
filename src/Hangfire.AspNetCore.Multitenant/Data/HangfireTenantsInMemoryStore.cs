using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public class HangfireTenantsInMemoryStore : IHangfireTenantsStore
    {
        public List<HangfireTenant> Tenants = new List<HangfireTenant>();

        public Task<IEnumerable<HangfireTenant>> GetAllTenantsAsync()
        {
            return Task.FromResult(Tenants.ToList().OrderBy(t => t.Id).Cast<HangfireTenant>());
        }

        public Task<HangfireTenant> GetTenantByIdAsync(object id)
        {
            var tenant = Tenants.FirstOrDefault(t => t.Id == id.ToString());
            return Task.FromResult(tenant);
        }
    }
}
