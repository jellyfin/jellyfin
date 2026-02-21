using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes orphaned extras (items with OwnerId pointing to non-existent items).
/// Must run before EF migrations that add FK constraints on OwnerId.
/// </summary>
[JellyfinMigration("2026-01-13T23:00:00", nameof(CleanupOrphanedExtras), Stage = JellyfinMigrationStageTypes.CoreInitialisation)]
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
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="channelManager">The channel manager.</param>
    /// <param name="recordingsManager">The recordings manager.</param>
    /// <param name="mediaSourceManager">The media source manager.</param>
    /// <param name="mediaSegmentManager">The media segments manager.</param>
    /// <param name="configurationManager">The configuration manager.</param>
    public CleanupOrphanedExtras(
        IStartupLogger<CleanupOrphanedExtras> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILibraryManager libraryManager,
        IItemRepository itemRepository,
        IChannelManager channelManager,
        IRecordingsManager recordingsManager,
        IMediaSourceManager mediaSourceManager,
        IMediaSegmentManager mediaSegmentManager,
        IServerConfigurationManager configurationManager)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _libraryManager = libraryManager;
        BaseItem.LibraryManager ??= libraryManager;
        BaseItem.ItemRepository ??= itemRepository;
        BaseItem.ChannelManager ??= channelManager;
        BaseItem.MediaSourceManager ??= mediaSourceManager;
        BaseItem.MediaSegmentManager ??= mediaSegmentManager;
        BaseItem.ConfigurationManager ??= configurationManager;
        Video.RecordingsManager ??= recordingsManager;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var orphanedItemIds = await context.BaseItems
                .Where(b => b.OwnerId.HasValue && !b.OwnerId.Value.Equals(Guid.Empty))
                .Where(b => !context.BaseItems.Any(parent => parent.Id.Equals(b.OwnerId!.Value)))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (orphanedItemIds.Count == 0)
            {
                _logger.LogInformation("No orphaned extras found, skipping migration.");
                return;
            }

            _logger.LogInformation("Found {Count} orphaned extras to remove", orphanedItemIds.Count);

            var deleteOptions = new DeleteOptions
            {
                DeleteFileLocation = false // Extras don't have their own media files
            };

            var deletedCount = 0;
            foreach (var itemId in orphanedItemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = _libraryManager.GetItemById(itemId);
                if (item is null)
                {
                    _logger.LogDebug("Item {ItemId} not found in library, may have been already deleted", itemId);
                    continue;
                }

                try
                {
                    _libraryManager.DeleteItem(item, deleteOptions, notifyParentItem: false);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete orphaned item {ItemId} ({ItemName})", item.Id, item.Name);
                }
            }

            _logger.LogInformation("Successfully removed {Count} orphaned extras", deletedCount);
        }
    }
}
