using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public interface IHangfireTenantSetup
    {
        Task OnTenantAdded(HangfireTenant tenant);
        Task OnTenantRemoved(HangfireTenant tenant);
        Task OnTenantUpdated(HangfireTenant updatedTenant, HangfireTenant oldTenant);
    }
}
