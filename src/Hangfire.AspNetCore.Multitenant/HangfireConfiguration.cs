using System.Data.Common;

namespace Hangfire.AspNetCore.Multitenant
{
    public class HangfireConfiguration
    {
        public bool Enabled { get; set; } = true;
        public DbInitialiation DbInitialiation { get; set; } = DbInitialiation.EnsureDbAndTablesCreatedAndHeavyMigrations;
        public string ServerName { get; set; } = "web-background";
        public string ConnectionString { get; set; } = ""; //InMemory
        public string SchemaName { get; set; } = "HangFire";
        public bool EnableLongPolling { get; set; } = false;

        public DbConnection ExistingConnection { get; set; }
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
