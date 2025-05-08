#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
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
        await CleanDeadItems(cancellationToken, progress).ConfigureAwait(false);
    }

    private async Task CleanDeadItems(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            HasDeadParentId = true
        });

        var numComplete = 0;
        var numItems = itemIds.Count + 1;

        _logger.LogDebug("Cleaning {Number} items with dead parent links", numItems);

        foreach (var itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = _libraryManager.GetItemById(itemId);
            if (item is not null)
            {
                _logger.LogInformation("Cleaning item {Item} type: {Type} path: {Path}", item.Name, item.GetType().Name, item.Path ?? string.Empty);

                foreach (var mediaSource in item.GetMediaSources(false))
                {
                    // Delete extracted subtitles
                    try
                    {
                        var subtitleFolder = _pathManager.GetSubtitleFolderPath(mediaSource.Id);
                        if (Directory.Exists(subtitleFolder))
                        {
                            Directory.Delete(subtitleFolder, true);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("Failed to remove subtitle cache folder for {Item}: {Exception}", item.Id, e.Message);
                    }

                    // Delete extracted attachments
                    try
                    {
                        var attachmentFolder = _pathManager.GetAttachmentFolderPath(mediaSource.Id);
                        if (Directory.Exists(attachmentFolder))
                        {
                            Directory.Delete(attachmentFolder, true);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("Failed to remove attachment cache folder for {Item}: {Exception}", item.Id, e.Message);
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

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            // await using (transaction.ConfigureAwait(false))
            // {
                await context.ItemValues.Where(e => e.BaseItemsMap!.Count == 0).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                // await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            // }
        }

        progress.Report(100);
    }
}
