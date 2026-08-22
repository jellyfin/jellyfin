using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Class StudiosValidator.
/// </summary>
public class StudiosValidator
{
    /// <summary>
    /// The library manager.
    /// </summary>
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<StudiosValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StudiosValidator" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public StudiosValidator(ILibraryManager libraryManager, ILogger<StudiosValidator> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Runs the specified progress.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var studios = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Studio]
        });

        var numComplete = 0;
        var count = studios.Count;
        var refreshed = 0;

        foreach (var item in studios)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (item.DateLastRefreshed == default)
                {
                    await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                    refreshed++;
                }
            }
            catch (OperationCanceledException)
            {
                // Don't clutter the log
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing {StudioName}", item.Name);
            }

            numComplete++;
            double percent = numComplete;
            percent /= count;
            percent *= 100;

            progress.Report(percent);
        }

        _logger.LogInformation("Refreshed metadata for {RefreshedCount} new studios out of {TotalCount} total", refreshed, count);

        var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Studio],
            IsDeadStudio = true,
            IsLocked = false
        });

        // An unpopulated link table reads as every studio being unused, and deleting one takes its
        // artwork with it.
        var totalStudios = _libraryManager.GetCount(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Studio],
            IsLocked = false
        });

        if (totalStudios > 0 && deadEntities.Count == totalStudios)
        {
            _logger.LogWarning("Every studio looks unused, which means the links are missing rather than the studios. Skipping cleanup");
            progress.Report(100);
            return;
        }

        foreach (var item in deadEntities)
        {
            _logger.LogInformation("Deleting dead {ItemType} {ItemId} {ItemName}", item.GetType().Name, item.Id.ToString("N", CultureInfo.InvariantCulture), item.Name);
        }

        _libraryManager.DeleteItemsUnsafeFast(deadEntities, deleteSourceFiles: true);

        progress.Report(100);
    }
}
