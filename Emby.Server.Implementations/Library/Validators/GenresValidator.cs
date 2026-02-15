using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
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
    private readonly IItemRepository _itemRepo;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<GenresValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenresValidator"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="itemRepo">The item repository.</param>
    public GenresValidator(ILibraryManager libraryManager, ILogger<GenresValidator> logger, IItemRepository itemRepo)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _itemRepo = itemRepo;
    }

    /// <summary>
    /// Runs the specified progress.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var names = _itemRepo.GetGenreNames();

        var numComplete = 0;
        var count = names.Count;

        foreach (var name in names)
        {
            try
            {
                var item = _libraryManager.GetGenre(name);

                await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Don't clutter the log
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing {GenreName}", name);
            }

            numComplete++;
            double percent = numComplete;
            percent /= count;
            percent *= 100;

            progress.Report(percent);
        }

        var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Genre, BaseItemKind.MusicGenre],
            IsDeadGenre = true,
            IsLocked = false
        });

        foreach (var item in deadEntities)
        {
            _logger.LogInformation("Deleting dead {ItemType} {ItemId} {ItemName}", item.GetType().Name, item.Id.ToString("N", CultureInfo.InvariantCulture), item.Name);

            _libraryManager.DeleteItem(
                item,
                new DeleteOptions
                {
                    DeleteFileLocation = false
                },
                false);
        }

        progress.Report(100);
    }
}
