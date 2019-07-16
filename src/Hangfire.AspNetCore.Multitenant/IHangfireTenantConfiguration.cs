using Autofac.Multitenant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.AspNetCore.Multitenant
{
    public interface IHangfireTenantConfiguration
    {
        object TenantId { get; }
        void ConfigureServices(ConfigurationActionBuilder services, IConfiguration configuration, IHostingEnvironment hostingEnvironment);
        void ConfigureHangfireJobs(IRecurringJobManager recurringJobManager, IConfiguration configuration, IHostingEnvironment hostingEnvironment);
    }
}
