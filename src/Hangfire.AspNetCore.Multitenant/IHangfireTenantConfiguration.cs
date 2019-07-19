using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant
{
    public interface IHangfireTenantConfiguration
    {
        object TenantId { get; }
        void ConfigureServices(HangfireTenant tenant, IServiceCollection services, IConfiguration configuration, IHostingEnvironment hostingEnvironment);

        void ConfigureHangfireDashboard(DashboardOptions options);

        void ConfigureHangfireServer(BackgroundJobServerOptions options);

        Task InitializeAsync(HangfireTenant tenant, IServiceProvider scope, IConfiguration configuration, IHostingEnvironment hostingEnvironment);
        void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager, IConfiguration configuration, IHostingEnvironment hostingEnvironment);
    }
}
