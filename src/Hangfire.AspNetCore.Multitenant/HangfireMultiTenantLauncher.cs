using Autofac.Multitenant;
using Hangfire.Initialization;
using Hangfire.Server;
using System;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class HangfireMultiTenantLauncher
    {
        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(
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

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(
            object tenantId,
            string serverName,
            JobStorage storage,
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

            var backgroundServerOptions = new BackgroundJobServerOptions()
            {
                ServerName = serverName,
                Queues = new string[] { serverName, "default" }
            };

            return HangfireLauncher.StartHangfireServer(
                backgroundServerOptions,
                storage,
                newConfig);
        }
    }
}
