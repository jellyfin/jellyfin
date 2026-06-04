#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Data;

public class CleanDatabaseScheduledTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CleanDatabaseScheduledTask> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IPathManager _pathManager;

    public CleanDatabaseScheduledTask(
        ILibraryManager libraryManager,
        ILogger<CleanDatabaseScheduledTask> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IPathManager pathManager)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _dbProvider = dbProvider;
        _pathManager = pathManager;
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var deadItemsProgress = new Progress<double>(val => progress.Report(val * 0.8));
        await CleanDeadItems(cancellationToken, deadItemsProgress).ConfigureAwait(false);

        var playlistProgress = new Progress<double>(val => progress.Report(80 + (val * 0.2)));
        await CleanOrphanedFilePlaylistsAsync(cancellationToken, playlistProgress).ConfigureAwait(false);
    }

    private async Task CleanDeadItems(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            HasDeadParentId = true
        });

        var numComplete = 0;
        var numItems = itemIds.Count + 1;

        _logger.LogDebug("Cleaning {Number} items with dead parents", numItems);

        IProgress<double> subProgress = new Progress<double>((val) => progress.Report(val / 2));

        foreach (var itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = _libraryManager.GetItemById(itemId);
            if (item is not null)
            {
                _logger.LogInformation("Cleaning item {Item} type: {Type} path: {Path}", item.Name, item.GetType().Name, item.Path ?? string.Empty);

                foreach (var mediaSource in item.GetMediaSources(false))
                {
                    // Delete extracted data
                    var mediaSourceItem = _libraryManager.GetItemById(mediaSource.Id);
                    if (mediaSourceItem is null)
                    {
                        continue;
                    }

                    var extractedDataFolders = _pathManager.GetExtractedDataPaths(mediaSourceItem);
                    foreach (var folder in extractedDataFolders)
                    {
                        if (Directory.Exists(folder))
                        {
                            try
                            {
                                Directory.Delete(folder, true);
                            }
                            catch (Exception e)
                            {
                                _logger.LogWarning("Failed to remove {Folder}: {Exception}", folder, e.Message);
                            }
                        }
                    }
                }

                // Delete item
                _libraryManager.DeleteItem(item, new DeleteOptions
                {
                    DeleteFileLocation = false
                });
            }

            numComplete++;
            double percent = numComplete;
            percent /= numItems;
            subProgress.Report(percent * 100);
        }

        subProgress = new Progress<double>((val) => progress.Report((val / 2) + 50));
        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                await context.ItemValues.Where(e => e.BaseItemsMap!.Count == 0).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                subProgress.Report(50);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                subProgress.Report(100);
            }
        }

        progress.Report(100);
    }

    private async Task CleanOrphanedFilePlaylistsAsync(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var playlists = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Playlist],
            Recursive = true
        }).OfType<Playlist>().ToList();

        var numComplete = 0;
        var numItems = Math.Max(playlists.Count, 1);

        foreach (var playlist in playlists)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (playlist.IsFile && !File.Exists(playlist.Path))
            {
                _logger.LogInformation("Removing file-based playlist {Name} because source file {Path} no longer exists", playlist.Name, playlist.Path);
                _libraryManager.DeleteItem(playlist, new DeleteOptions { DeleteFileLocation = false });
            }

            numComplete++;
            progress.Report((double)numComplete / numItems * 100);
        }

        progress.Report(100);
    }
}
