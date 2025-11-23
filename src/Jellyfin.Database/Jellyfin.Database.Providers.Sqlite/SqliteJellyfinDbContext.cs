using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Interfaces;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// SQLite-specific DbContext that extends JellyfinDbContext with FTS5 entities.
/// </summary>
/// <param name="options">The database context options.</param>
/// <param name="logger">Logger.</param>
/// <param name="jellyfinDatabaseProvider">The provider for the database engine specific operations.</param>
/// <param name="entityFrameworkCoreLocking">The locking behavior.</param>
public class SqliteJellyfinDbContext(
    DbContextOptions options,
    ILogger<JellyfinDbContext> logger,
    IJellyfinDatabaseProvider jellyfinDatabaseProvider,
    IEntityFrameworkCoreLockingBehavior entityFrameworkCoreLocking)
    : JellyfinDbContext(options, logger, jellyfinDatabaseProvider, entityFrameworkCoreLocking)
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the FTS5 virtual table for full-text search.
    /// </summary>
    public DbSet<BaseItemFtsEntity> BaseItemFts => Set<BaseItemFtsEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure SQLite-specific entities
        modelBuilder.ApplyConfiguration(new ModelConfiguration.BaseItemFtsEntityConfiguration());
    }
}
