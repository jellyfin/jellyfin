#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Data;

public class CleanDatabaseScheduledTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CleanDatabaseScheduledTask> _logger;
    private readonly IPathManager _pathManager;

    public CleanDatabaseScheduledTask(
        ILibraryManager libraryManager,
        ILogger<CleanDatabaseScheduledTask> logger,
        IPathManager pathManager)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _pathManager = pathManager;
    }

    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var deadItemsProgress = new Progress<double>(val => progress.Report(val * 0.8));
        CleanDeadItems(cancellationToken, deadItemsProgress);

        var playlistProgress = new Progress<double>(val => progress.Report(80 + (val * 0.2)));
        CleanOrphanedFilePlaylists(cancellationToken, playlistProgress);

        return Task.CompletedTask;
    }

    private void CleanDeadItems(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            HasDeadParentId = true
        });

        var numComplete = 0;
        var numItems = itemIds.Count + 1;

        _logger.LogDebug("Cleaning {Number} items with dead parents", numItems);

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
            progress.Report(percent * 100);
        }

        progress.Report(100);
    }

    private void CleanOrphanedFilePlaylists(CancellationToken cancellationToken, IProgress<double> progress)
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
