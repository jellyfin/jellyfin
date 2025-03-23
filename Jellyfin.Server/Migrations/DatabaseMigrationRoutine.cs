using System;
using Jellyfin.Server.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Defines a migration that operates on the Database.
/// </summary>
internal abstract class DatabaseMigrationRoutine : IMigrationRoutine
{
    protected DatabaseMigrationRoutine(IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        DbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract bool PerformOnNewInstall { get; }

    protected IDbContextFactory<JellyfinDbContext> DbContextFactory { get; }

    /// <inheritdoc />
    public abstract void Perform();
}
