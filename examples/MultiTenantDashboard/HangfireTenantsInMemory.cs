using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTenantDashboard
{
    public class HangfireTenantsInMemory : HangfireTenantsInMemoryStore
    {
        public HangfireTenantsInMemory(ILogger<HangfireTenantsInMemory> logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
        {
            CacheExpiryMinutes = 1;
        }

        public override async Task InitializeTenantsAsync(CancellationToken cancellationToken = default)
        {
            //Server=(localdb)\\mssqllocaldb;Database=HangfireDatabase;Trusted_Connection=True;MultipleActiveResultSets=true; will use an SQLServer database.
            //"" will start a new hangfire server in memory.
            //Data Source=tenant1.db; will start a new SQLite hangfire server.
            //Data Source=:memory:; will start a new SQLite InMemory hangfire server.
            //null will mean tenant does not get a hangfire server.

            var sharedConnectionString = "Server=(localdb)\\mssqllocaldb;Database=HangfireMultitenant;Trusted_Connection=True;MultipleActiveResultSets=true;";
            //var sharedConnectionString = "Data Source=:memory:;";
            //var sharedConnectionString = "";

            Tenants = new List<HangfireTenant>(){
                new HangfireTenant(){ Id ="tenant0", HostNames = new string []{ "" }, DbInitialiation = DbInitialiation.EnsureTablesDeletedThenEnsureDbAndTablesCreated, HangfireConnectionString = sharedConnectionString, HangfireSchemaName ="tenant0"}
                .AddEnvionment("production", c => {c.HangfireConnectionString = "Data Source=tenant0.db;"; c.DbInitialiation = DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations; })
                };

            int tenantCount = 20;
            for (int i = 1; i <= tenantCount; i++)
            {
                Tenants.Add(new HangfireTenant() { Id = $"tenant{i}", HostNames = new string[] { $"tenant{i}.*" }, DbInitialiation = DbInitialiation.EnsureTablesDeletedThenEnsureDbAndTablesCreated, HangfireConnectionString = sharedConnectionString, HangfireSchemaName = $"tenant{i}" }
                .AddEnvionment("production", c => { c.HangfireConnectionString = $"Data Source=tenant{i}.db;"; c.DbInitialiation = DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations; }));
            }

            await base.InitializeTenantsAsync();
        }

        //Kestral is good for testing multitenant apps as localhost subdomains works without needing to modify hosts file. By default appsettings.json "AllowedHosts": "*".

        //-- If using IIS Express will need to update hosts file.
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