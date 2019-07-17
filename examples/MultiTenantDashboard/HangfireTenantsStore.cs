using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.Initialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantDashboard
{
    public class HangfireTenantsStore : HangfireTenantsInMemoryStore
    {
        
        private readonly IServiceProvider _serviceProvider;
        public HangfireTenantsStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            CacheExpiryMinutes = 1;
        }

        public override async Task InitializeTenantsAsync()
        {
            Tenants = new List<HangfireTenant>(){
                new HangfireTenant(){ Id ="default", HostNames = new string []{ "*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} },
                new HangfireTenant(){ Id ="tenant1", HostNames = new string []{ "tenant1.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }}} ,
                new HangfireTenant(){ Id ="tenant2", HostNames = new string []{ "tenant2.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant3", HostNames = new string []{ "tenant3.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant4", HostNames = new string []{ "tenant4.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant5", HostNames = new string []{ "tenant5.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant6", HostNames = new string []{ "tenant6.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant7", HostNames = new string []{ "tenant7.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant8", HostNames = new string []{ "tenant8.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant9", HostNames = new string []{ "tenant9.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
                new HangfireTenant(){ Id ="tenant10", HostNames = new string []{ "tenant10.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} },
            };

            await base.InitializeTenantsAsync();
        }

        public override void OnTenantAdded(HangfireTenant tenant)
        {
            var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            var environment = _serviceProvider.GetRequiredService<IHostingEnvironment>();
            var applicationLifetime = _serviceProvider.GetRequiredService<IApplicationLifetime>();

            var tenantsStore = _serviceProvider.GetRequiredService<IHangfireTenantsStore>();
            var tenantConfigurations = _serviceProvider.GetServices<IHangfireTenantConfiguration>();

            var tenantInitializer = tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == tenant.Id);

            var actionBuilder = new ConfigurationActionBuilder();
            var services = new ServiceCollection();

            if (tenantInitializer != null)
            {
                tenantInitializer.ConfigureServices(services, configuration, environment);
            }

            if (tenant.HangfireConnectionString != null)
            {
                var serverDetails = HangfireMultiTenantLauncher.StartHangfireServer(tenant.Id, "web-background", tenant.HangfireConnectionString, multitenantContainer, options => { options.ApplicationLifetime = applicationLifetime; options.AdditionalProcesses = tenant.AdditionalProcesses; });

                tenant.Storage = serverDetails.Storage;

                if (tenantInitializer != null)
                {
                    tenantInitializer.ConfigureHangfireJobs(serverDetails.recurringJobManager, configuration, environment);
                }

                services.AddSingleton(serverDetails.recurringJobManager);
                services.AddSingleton(serverDetails.backgroundJobClient);
                //actionBuilder.Add(b => b.RegisterInstance(serverDetails.recurringJobManager).As<IRecurringJobManager>().SingleInstance());
                //actionBuilder.Add(b => b.RegisterInstance(serverDetails.backgroundJobClient).As<IBackgroundJobClient>().SingleInstance());
            }
            else
            {
                services.AddSingleton<IRecurringJobManager>(sp => null);
                services.AddSingleton<IBackgroundJobClient>(sp => null);
                //actionBuilder.Add(b => b.RegisterInstance<IRecurringJobManager>(null).As<IRecurringJobManager>().SingleInstance());
                //actionBuilder.Add(b => b.RegisterInstance<IBackgroundJobClient>(null).As<IBackgroundJobClient>().SingleInstance());
            }

            actionBuilder.Add(b => b.Populate(services));
            multitenantContainer.ConfigureTenant(tenant.Id, actionBuilder.Build());
        }

        public override void OnTenantRemoved(HangfireTenant tenant)
        {
            var multitenantContainer = _serviceProvider.GetRequiredService<MultitenantContainer>();
            multitenantContainer.RemoveTenant(tenant.Id);
        }

        //C:\WINDOWS\system32\drivers\etc\hosts
        //127.0.0.1 tenant1.localhost
        //127.0.0.1 tenant2.localhost
        //127.0.0.1 tenant3.localhost
        //127.0.0.1 tenant4.localhost
        //127.0.0.1 tenant5.localhost
        //127.0.0.1 tenant6.localhost
        //127.0.0.1 tenant7.localhost
        //127.0.0.1 tenant8.localhost
        //127.0.0.1 tenant9.localhost
        //127.0.0.1 tenant10.localhost
    }
}