using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.AspNetCore.Multitenant
{
    public class HangfireTenant
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; } = true;

        public string[] RequestIpAddresses { set { DefaultConfig.RequestIpAddresses = value; } }
        public string[] HostNames { set { DefaultConfig.HostNames = value; } }
        public DbInitialiation DbInitialiation { set { DefaultConfig.DbInitialiation = value; } }

        public string HangfireServerName { set { DefaultConfig.HangfireServerName = value; } }
        public string HangfireConnectionString { set { DefaultConfig.HangfireConnectionString = value; } }
        public string HangfireSchemaName { set { DefaultConfig.HangfireSchemaName = value; } }
        public bool HangfireEnableLongPolling { set { DefaultConfig.HangfireEnableLongPolling = value; } }

        public HangfireTenantEnvironmentConfig DefaultConfig = new HangfireTenantEnvironmentConfig()
        {
            DbInitialiation = DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations,
            HangfireServerName = "web-background",
            HangfireConnectionString = "", //InMemory default
            HangfireSchemaName = "HangFire",
            HangfireEnableLongPolling = false
        };

        public Dictionary<string, HangfireTenantEnvironmentConfig> EnvironmentConfigs = new Dictionary<string, HangfireTenantEnvironmentConfig>(StringComparer.OrdinalIgnoreCase);

        public HangfireTenantEnvironmentConfig GetEnvironmentConfig(string environment = null)
        {
            var config = new HangfireTenantEnvironmentConfig()
            {
                RequestIpAddresses = DefaultConfig.RequestIpAddresses,
                HostNames = DefaultConfig.HostNames,
                DbInitialiation = DefaultConfig.DbInitialiation,
                HangfireServerName = DefaultConfig.HangfireServerName,
                HangfireConnectionString = DefaultConfig.HangfireConnectionString,
                HangfireSchemaName = DefaultConfig.HangfireSchemaName,
                HangfireEnableLongPolling = DefaultConfig.HangfireEnableLongPolling
            };

            if(EnvironmentConfigs.ContainsKey(environment))
            {
                var environmentConfig = EnvironmentConfigs[environment];
                config.RequestIpAddresses = environmentConfig.RequestIpAddresses ?? config.RequestIpAddresses;
                config.HostNames = environmentConfig.HostNames ?? config.HostNames;
                config.DbInitialiation = environmentConfig.DbInitialiation.HasValue ? environmentConfig.DbInitialiation.Value : config.DbInitialiation;
                config.HangfireServerName = environmentConfig.HangfireServerName != null ? environmentConfig.HangfireServerName : config.HangfireServerName;
                config.HangfireConnectionString = environmentConfig.HangfireConnectionString != "null" ? environmentConfig.HangfireConnectionString : config.HangfireConnectionString;
                config.HangfireSchemaName = environmentConfig.HangfireSchemaName != null ? environmentConfig.HangfireSchemaName : config.HangfireSchemaName;
                config.HangfireEnableLongPolling = environmentConfig.HangfireEnableLongPolling.HasValue ? environmentConfig.HangfireEnableLongPolling.Value : config.HangfireEnableLongPolling;
            }

            return config;
        }
    }

    public static class HangfireTenantExtensions
    {
        public static HangfireTenant AddEnvionment(this HangfireTenant tenant, string environmentName, Action<HangfireTenantEnvironmentConfig> environmentConfig)
        {
            var config = new HangfireTenantEnvironmentConfig();
            environmentConfig(config);

            tenant.EnvironmentConfigs.Add(environmentName, config);

            return tenant;
        }
    }

    public class HangfireTenantEnvironmentConfig
    {
        public string[] RequestIpAddresses { get; set; }
        public string[] HostNames { get; set; }
        public Nullable<DbInitialiation> DbInitialiation { get; set; }

        public string HangfireServerName { get; set; }
        public string HangfireConnectionString { get; set; } = "null";
        public string HangfireSchemaName { get; set; }

        public Nullable<bool> HangfireEnableLongPolling { get; set; }

        public bool IpAddressAllowed(string ip)
        {
            return
                RequestIpAddresses == null
                || RequestIpAddresses.Length == 0
                || RequestIpAddresses.Where(i => !i.Contains("*") || i.EndsWith("*")).Any(i => ip.StartsWith(i.Replace("*", "")))
                || RequestIpAddresses.Where(i => i.StartsWith("*")).Any(i => ip.EndsWith(i.Replace("*", "")));
        }
    }

    public enum DbInitialiation
    {
        None = 0,
        PrepareSchemaIfNecessary = 1,
        PrepareSchemaIfNecessaryAndHeavyMigrations = 2,
        EnsureTablesDeletedThenEnsureDbAndTablesCreated = 3,
        EnsureDbAndTablesCreated = 4,
        EnsureDbAndTablesCreatedAndHeavyMigrations = 5
    }
}
