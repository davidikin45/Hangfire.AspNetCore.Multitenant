using Hangfire.Annotations;
using Hangfire.Server;
using System;

namespace MultiTenantDashboard.TenantConfiguration.Default
{
    public class BackgroundProcess : IBackgroundProcess
    {
        //https://www.hangfire.io/overview.html
        public void Execute([NotNull] BackgroundProcessContext context)
        {

            context.Wait(TimeSpan.FromHours(1));
        }
    }
}
