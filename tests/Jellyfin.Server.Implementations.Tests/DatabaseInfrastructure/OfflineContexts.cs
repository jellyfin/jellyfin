using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Contexts of the providers that are never connected, for checks of the model and the migrations assemblies.
/// </summary>
/// <remarks>
/// EF Core keeps one internal service provider for each configuration of options in a cache for the whole process, and
/// throws once a process asks for more than twenty of them, as that is what a context that builds new options for each
/// instance does. These contexts are built for many server versions and providers on purpose, so they are left out of
/// that cache: each builds its own service provider and model, and the cache, with the count EF Core keeps of it, only
/// holds the configurations that contexts on a database use. A build without the cache is still checked against the
/// count, but not added to it. EF Core fails only once twenty are cached, so in a short run it is
/// <see cref="InternalServiceProviderTests"/> that catches options that differ for every context.
/// </remarks>
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

        return Create(builder, provider);
    }

    public static JellyfinDbContext MySql(string serverVersion = "8.4-mysql")
    {
        var provider = new MySqlDatabaseProvider(NullLogger<MySqlDatabaseProvider>.Instance);
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        provider.Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-MySQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = "Server=localhost;Database=jellyfin_offline",
                    Options = [new CustomDatabaseOption { Key = "server-version", Value = serverVersion }]
                }
            });

        return Create(builder, provider);
    }

    public static JellyfinDbContext Sqlite()
    {
        var provider = new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance);
#pragma warning disable RS0030 // Do not use banned APIs: The context is only inspected offline and never opens a database.
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite("Data Source=:memory:", sqlite => sqlite.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly));
#pragma warning restore RS0030

        return Create(builder, provider);
    }

    private static JellyfinDbContext Create(DbContextOptionsBuilder<JellyfinDbContext> builder, IJellyfinDatabaseProvider provider)
    {
        builder.EnableServiceProviderCaching(false);
        return new JellyfinDbContext(builder.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
