using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MultiTenantDashboard.TenantConfiguration.Default
{
    public class Configuration : IHangfireTenantConfiguration
    {
        public object TenantId => "tenant0";

        public void ConfigureServices(HangfireTenant tenant, IServiceCollection services, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            services.AddSingleton<IBackgroundProcess, BackgroundProcess>();
        }

        public void ConfigureHangfireDashboard(DashboardOptions options)
        {
         
        }
        public void ConfigureHangfireServer(BackgroundJobServerOptions options)
        {
            
        }

        public Task InitializeAsync(HangfireTenant tenant, IServiceProvider scope, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            return Task.CompletedTask;
        }

        public void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            recurringJobManager.AddOrUpdate("check-link", Job.FromExpression<Job1>(m => m.Execute()), Cron.Minutely(), new RecurringJobOptions());
            recurringJobManager.Trigger("check-link");
        }

    }
}
