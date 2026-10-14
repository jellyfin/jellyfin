using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Server.Implementations.Tests.Postgres;
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
internal sealed record MigrationProvider(
    string Name,
    Func<JellyfinDbContext> CreateContext,
    string? BaselineId = null,
    string? CoversThroughReferenceId = null)
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
        new("PostgreSQL", OfflineContexts.Postgres, PostgresMigrationBaseline.MigrationId, PostgresMigrationBaseline.CoversThroughSqliteMigration)
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
}
