using Autofac.AspNetCore.Extensions;
using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace MultiTenantDashboardAspNetCore3.TenantConfiguration.tenant5
{
    public class Config : HangfireTenantConfiguration
    {
        public override object TenantId => "tenant5";

        public override IEnumerable<string> HostNames => new string[] { };

        public override void ConfigureBackgroundProcesses(TenantBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<IBackgroundProcess, BackgroundProcess>();
        }

        public override void ConfigureAppConfiguration(TenantBuilderContext context, IConfigurationBuilder builder)
        {
           
        }

        public override void ConfigureHangfireDashboard(TenantBuilderContext context, DashboardOptions options)
        {
           
        }

        public override void ConfigureHangfireServer(TenantBuilderContext context, BackgroundJobServerOptions options)
        {
           
        }

        public override void ConfigureHangfire(TenantBuilderContext context, HangfireConfiguration configuration)
        {
          
        }

        public override void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.AddOrUpdate((string)"check-link", Job.FromExpression<Job1>(m => m.Execute()), Cron.Minutely(), new RecurringJobOptions());
            recurringJobManager.Trigger("check-link");
        }
    }
}
