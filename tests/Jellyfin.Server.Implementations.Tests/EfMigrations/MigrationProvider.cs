using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// A database provider with its own migrations. Adding a provider means adding one row to <see cref="All"/>.
/// </summary>
/// <param name="Name">The name of the provider, as shown in the test names and messages.</param>
/// <param name="CreateContext">Creates a context of the provider that is never connected.</param>
/// <param name="BaselineId">The id of the migration that creates the whole schema. Required for every provider but the reference provider.</param>
/// <param name="CoversThroughReferenceId">The last migration of the reference provider that the baseline already contains. Required for every provider but the reference provider.</param>
/// <param name="ServerVersion">The version of the server that the model is built for, as the <c>server-version</c> option names it, for a provider whose model depends on it.</param>
internal sealed record MigrationProvider(
    string Name,
    Func<JellyfinDbContext> CreateContext,
    string? BaselineId = null,
    string? CoversThroughReferenceId = null,
    string? ServerVersion = null)
{
    /// <summary>
    /// The command that generates the migrations of every provider.
    /// </summary>
    public const string Command = "dev/add-migration";

    /// <summary>
    /// Gets the providers. The first one is the reference provider, whose migrations the others mirror.
    /// </summary>
    public static IReadOnlyList<MigrationProvider> All { get; } =
    [
        new("SQLite", OfflineContexts.Sqlite),
        new("PostgreSQL", OfflineContexts.Postgres, PostgresMigrationBaseline.MigrationId, PostgresMigrationBaseline.CoversThroughSqliteMigration),
        // MySQL and MariaDB share one set of migrations, but the model snapshot of a migration depends on the server version,
        // so the model is checked against the oldest supported version of each.
        MySqlFamily("MySQL 8.4", "8.4-mysql"),
        MySqlFamily("MariaDB 11.4.7", "11.4.7-mariadb"),
        // The snapshot is built for the oldest supported version, but the provider has no newest supported version and builds
        // its model for the server it finds, so the model is checked against newer releases of each as well.
        MySqlFamily("MySQL 9.7", "9.7.0-mysql"),
        MySqlFamily("MariaDB 11.8", "11.8.0-mariadb"),
        MySqlFamily("MariaDB 12.3", "12.3.0-mariadb"),
        new("SQL Server", OfflineContexts.SqlServer, SqlServerMigrationBaseline.MigrationId, SqlServerMigrationBaseline.CoversThroughSqliteMigration)
    ];

    /// <summary>
    /// Gets the reference provider.
    /// </summary>
    public static MigrationProvider Reference => All[0];

    /// <summary>
    /// Gets a value indicating whether this provider is the reference provider.
    /// </summary>
    public bool IsReference => ReferenceEquals(this, Reference);

    /// <summary>
    /// Finds a provider by name.
    /// </summary>
    /// <param name="name">The name of the provider.</param>
    /// <returns>The provider.</returns>
    public static MigrationProvider Get(string name) => All.Single(e => e.Name == name);

    /// <summary>
    /// Gets the ids of the migrations of this provider in the order they are applied.
    /// </summary>
    /// <returns>The migration ids.</returns>
    public string[] MigrationIds()
    {
        using var context = CreateContext();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }

    private static MigrationProvider MySqlFamily(string name, string serverVersion)
        => new(name, () => OfflineContexts.MySql(serverVersion), MySqlMigrationBaseline.MigrationId, MySqlMigrationBaseline.CoversThroughSqliteMigration, serverVersion);
}
