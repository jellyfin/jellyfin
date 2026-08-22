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
/// Class GenresValidator.
/// </summary>
public class GenresValidator
{
    /// <summary>
    /// The library manager.
    /// </summary>
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<GenresValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenresValidator"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public GenresValidator(ILibraryManager libraryManager, ILogger<GenresValidator> logger)
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
        var genres = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Genre]
        });

        var numComplete = 0;
        var count = genres.Count;
        var refreshed = 0;

        foreach (var item in genres)
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
                _logger.LogError(ex, "Error refreshing {GenreName}", item.Name);
            }

            numComplete++;
            double percent = numComplete;
            percent /= count;
            percent *= 100;

            progress.Report(percent);
        }

        _logger.LogInformation("Refreshed metadata for {RefreshedCount} new genres out of {TotalCount} total", refreshed, count);

        var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Genre, BaseItemKind.MusicGenre],
            IsDeadGenre = true,
            IsLocked = false
        });

        // An unpopulated link table reads as every genre being unused, and deleting one takes its
        // artwork with it.
        var totalGenres = _libraryManager.GetCount(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Genre, BaseItemKind.MusicGenre],
            IsLocked = false
        });

        if (totalGenres > 0 && deadEntities.Count == totalGenres)
        {
            _logger.LogWarning("Every genre looks unused, which means the links are missing rather than the genres. Skipping cleanup");
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
