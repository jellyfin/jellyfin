using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes orphaned <c>BaseItems</c> rows whose <c>ParentId</c> references a non-existent parent.
/// Required for installations that ran with SQLite foreign key enforcement disabled and therefore
/// accumulated orphaned Season/Episode rows that resurrected deleted Series entries on every
/// library scan (see #16883).
/// </summary>
[JellyfinMigration("2026-05-22T12:00:00", nameof(CleanupOrphanedBaseItems))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class CleanupOrphanedBaseItems : IAsyncMigrationRoutine
{
    private const string DeleteOrphansSql = """
        DELETE FROM BaseItems
        WHERE ParentId IS NOT NULL
        AND NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
        """;

    private readonly IStartupLogger<CleanupOrphanedBaseItems> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupOrphanedBaseItems"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public CleanupOrphanedBaseItems(
        IStartupLogger<CleanupOrphanedBaseItems> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // Loop until the cleanup converges. With foreign key enforcement now enabled, a single
            // pass should be sufficient (cascade handles descendants), but iterating defends against
            // future schema changes that introduce additional self-referential relationships.
            var totalDeleted = 0;
            int deleted;
            do
            {
                deleted = await context.Database
                    .ExecuteSqlRawAsync(DeleteOrphansSql, cancellationToken)
                    .ConfigureAwait(false);
                totalDeleted += deleted;
            }
            while (deleted > 0);

            if (totalDeleted == 0)
            {
                _logger.LogInformation("No orphaned BaseItems rows found, skipping migration.");
            }
            else
            {
                _logger.LogInformation("Removed {Count} orphaned BaseItems rows", totalDeleted);
            }
        }
    }
}
