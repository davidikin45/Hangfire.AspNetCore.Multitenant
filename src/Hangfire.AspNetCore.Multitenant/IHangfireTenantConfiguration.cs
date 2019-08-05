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
        void ConfigureServices(HangfireTenant tenant, IServiceCollection services);

        void ConfigureHangfireDashboard(DashboardOptions options);

        void ConfigureHangfireServer(BackgroundJobServerOptions options);

        Task InitializeAsync(HangfireTenant tenant, IServiceProvider scope);
        void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager);
    }
}
