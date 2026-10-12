using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// Contexts of the providers that are never connected, for checks of the model and the migrations assemblies.
/// </summary>
internal static class OfflineContexts
{
    public static JellyfinDbContext Postgres()
    {
        var provider = new PostgresDatabaseProvider(NullLogger<PostgresDatabaseProvider>.Instance);
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        provider.Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-PostgreSQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = "Host=localhost;Database=jellyfin_offline"
                }
            });

        return new JellyfinDbContext(builder.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    public static JellyfinDbContext Sqlite()
    {
        var provider = new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance);
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite("Data Source=:memory:", sqlite => sqlite.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly));

        return new JellyfinDbContext(builder.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
