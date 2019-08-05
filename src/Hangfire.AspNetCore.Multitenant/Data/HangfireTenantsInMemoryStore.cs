using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public abstract class HangfireTenantsInMemoryStore : HangfireTenantsInMemoryCachingStore
    {
        public HangfireTenantsInMemoryStore(ILogger logger, IServiceProvider serviceProvider)
          : base(logger, serviceProvider)
        {

        }

        public static List<HangfireTenant> Tenants { get; set; } = new List<HangfireTenant>();

        public override Task<IEnumerable<HangfireTenant>> GetAllActiveTenantsFromStoreAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tenants.Where(t => t.Active).OrderBy(t => t.Id).ToList().Cast<HangfireTenant>());
        }
    }
}
