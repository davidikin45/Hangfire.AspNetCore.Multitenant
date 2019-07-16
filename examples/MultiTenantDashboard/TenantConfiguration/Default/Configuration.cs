using Autofac.Multitenant;
using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MultiTenantDashboard.TenantConfiguration.Default
{
    public class Configuration : IHangfireTenantConfiguration
    {
        public object TenantId => "default";

        public void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            recurringJobManager.AddOrUpdate("check-link", Job.FromExpression<Job1>(m => m.Execute()), Cron.Minutely(), new RecurringJobOptions());
            recurringJobManager.Trigger("check-link");
        }

        public void ConfigureServices(ConfigurationActionBuilder services, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
           
        }
    }
}
