using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    // The share that may look unused before the cleanup is read as a link problem instead. A library
    // that genuinely lost this many studios gets them on the next scan.
    private const double MaxDeadShare = 0.25;

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

        // A link table that lost its rows reads as studios being unused, and deleting one takes its
        // artwork with it. Past a small share of the library that is a link problem rather than a
        // studio problem, so the studios are left alone and the names logged for an admin to judge.
        var totalStudios = _libraryManager.GetCount(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Studio],
            IsLocked = false
        });

        if (totalStudios > 0 && deadEntities.Count > totalStudios * MaxDeadShare)
        {
            _logger.LogWarning(
                "{DeadCount} of {TotalCount} studios look unused, which reads as missing links rather than unused studios. Skipping cleanup of {Studios}",
                deadEntities.Count,
                totalStudios,
                string.Join(", ", deadEntities.Select(e => e.Name)));
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
