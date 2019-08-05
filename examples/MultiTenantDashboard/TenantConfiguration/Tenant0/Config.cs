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
    public class Config : IHangfireTenantConfiguration
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        public Config(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public object TenantId => "tenant0";

        public void ConfigureServices(HangfireTenant tenant, IServiceCollection services)
        {
            services.AddSingleton<IBackgroundProcess, BackgroundProcess>();
        }

        public void ConfigureHangfireDashboard(DashboardOptions options)
        {
         
        }
        public void ConfigureHangfireServer(BackgroundJobServerOptions options)
        {
            
        }

        public Task InitializeAsync(HangfireTenant tenant, IServiceProvider scope)
        {
            return Task.CompletedTask;
        }

        public void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.AddOrUpdate("check-link", Job.FromExpression<Job1>(m => m.Execute()), Cron.Minutely(), new RecurringJobOptions());
            recurringJobManager.Trigger("check-link");
        }

    }
}
