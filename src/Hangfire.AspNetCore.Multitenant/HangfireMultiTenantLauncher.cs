using Autofac.Multitenant;
using Hangfire.Initialization;
using Hangfire.Server;
using System;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class HangfireMultiTenantLauncher
    {
        public static (IBackgroundProcessingServer server, IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient, JobStorage Storage) StartHangfireServer(
            object tenantId,
            string serverName,
            string connectionString,
            MultitenantContainer mtc,
            Action<HangfireLauncherOptions> config = null
            )
        {
            Action<HangfireLauncherOptions> newConfig = (options) =>
            {
                if (config != null)
                    config(options);

                options.Activator = new MultiTenantJobActivator(mtc, tenantId);
            };

            return HangfireLauncher.StartHangfireServer(
                serverName,
                connectionString,
                newConfig);
        }
    }
}
