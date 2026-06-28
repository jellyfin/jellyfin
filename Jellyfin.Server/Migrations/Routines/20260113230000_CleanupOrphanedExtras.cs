using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes orphaned extras (items with OwnerId pointing to non-existent items).
/// Must run before EF migrations that add FK constraints on OwnerId.
/// </summary>
[JellyfinMigration("2026-01-13T23:00:00", nameof(CleanupOrphanedExtras), Stage = JellyfinMigrationStageTypes.AppInitialisation)]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class CleanupOrphanedExtras : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<CleanupOrphanedExtras> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupOrphanedExtras"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    public CleanupOrphanedExtras(
        IStartupLogger<CleanupOrphanedExtras> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var placeholderOwner = Guid.Parse("00000000-0000-0000-0000-000000000001");
#pragma warning disable RS0030 // Do not use banned APIs
            var orphanedItemIds = await context.BaseItems
                .Where(b => b.OwnerId.HasValue && b.OwnerId == placeholderOwner)
                .Select(b => new
                {
                    b.Id,
                    b.Path,
                    b.Type
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
#pragma warning restore RS0030 // Do not use banned APIs

            if (orphanedItemIds.Count == 0)
            {
                _logger.LogInformation("No orphaned extras found, skipping migration.");
                return;
            }

            _logger.LogInformation("Found {Count} orphaned extras to remove", orphanedItemIds.Count);

            // Resolve items for metadata path cleanup, then delete in batches so we never issue one
            // massive delete transaction and progress stays visible on large libraries.
            _logger.LogInformation("Deleting {Count} orphaned extras...", orphanedItemIds.Count);
            const int deleteBatchSize = 500;
            var deletedSoFar = 0;
            for (var offset = 0; offset < orphanedItemIds.Count; offset += deleteBatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = orphanedItemIds.GetRange(offset, Math.Min(deleteBatchSize, orphanedItemIds.Count - offset));
                var itemsToDelete = batch
                    .Select(itemId => BaseItemMapper.DeserializeBaseItem(
                        new Database.Implementations.Entities.BaseItemEntity()
                        {
                            Id = itemId.Id,
                            Path = itemId.Path,
                            Type = itemId.Type
                        },
                        _logger,
                        null,
                        true)!)
                    .ToList();

                _libraryManager.DeleteItemsUnsafeFast(itemsToDelete);

                deletedSoFar += batch.Count;
                _logger.LogInformation("Deleting orphaned extras: {Deleted}/{Total}", deletedSoFar, orphanedItemIds.Count);
            }

            _logger.LogInformation("Successfully removed {Count} orphaned extras", orphanedItemIds.Count);
        }
    }
}
