using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Providers.MySql.Migrations;

/// <summary>
/// The design time factory for <see cref="JellyfinDbContext"/>.
/// This is only used for the creation of migrations and not during runtime, and it never connects to a server.
/// </summary>
/// <remarks>
/// The server version is fixed, so that the migrations come out the same wherever they are generated. The same
/// migrations are applied to MySQL and to MariaDB.
/// </remarks>
internal sealed class MySqlDesignTimeJellyfinDbFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
{
    private const string DesignServerVersion = "8.4-mysql";

    public JellyfinDbContext CreateDbContext(string[] args)
    {
        var provider = new MySqlDatabaseProvider(NullLogger<MySqlDatabaseProvider>.Instance);
        var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();
        provider.Initialise(
            optionsBuilder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-MySQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = "Server=localhost;Database=jellyfin_design",
                    Options = [new CustomDatabaseOption { Key = "server-version", Value = DesignServerVersion }]
                }
            });

        return new JellyfinDbContext(
            optionsBuilder.Options,
            NullLogger<JellyfinDbContext>.Instance,
            provider,
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
