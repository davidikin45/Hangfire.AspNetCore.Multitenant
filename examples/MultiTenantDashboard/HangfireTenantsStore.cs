using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.Initialization.Attributes;
using System.Collections.Generic;

namespace MultiTenantDashboard
{
    public class HangfireTenantsStore : HangfireTenantsInMemoryStore
    {
        public HangfireTenantsStore()
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