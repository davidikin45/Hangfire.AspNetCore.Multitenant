using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public abstract class HangfireTenantsInMemoryStore : HangfireTenantsInMemoryCachingStore
    {
        public static List<HangfireTenant> Tenants { get; set; } = new List<HangfireTenant>();

        public override Task<IEnumerable<HangfireTenant>> GetAllActiveTenantsFromStoreAsync()
        {
            return Task.FromResult(Tenants.Where(t => t.Active).OrderBy(t => t.Id).ToList().Cast<HangfireTenant>());
        }
    }
}
