using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class ArtistsValidator.
    /// </summary>
    public class ArtistsValidator
    {
        /// <summary>
        /// The library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsValidator" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="itemRepo">The item repository.</param>
        public ArtistsValidator(ILibraryManager libraryManager, ILogger logger, IItemRepository itemRepo)
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
            var names = _itemRepo.GetAllArtistNames();

            var numComplete = 0;
            var count = names.Count;

            foreach (var name in names)
            {
                try
                {
                    var item = _libraryManager.GetArtist(name);

                    await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Don't clutter the log
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing {ArtistName}", name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;

                progress.Report(percent);
            }

            var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(MusicArtist).Name },
                IsDeadArtist = true,
                IsLocked = false
            }).Cast<MusicArtist>().ToList();

            foreach (var item in deadEntities)
            {
                if (!item.IsAccessedByName)
                {
                    continue;
                }

                _logger.LogInformation("Deleting dead {2} {0} {1}.", item.Id.ToString("N", CultureInfo.InvariantCulture), item.Name, item.GetType().Name);

                _libraryManager.DeleteItem(item, new DeleteOptions
                {
                    DeleteFileLocation = false

                }, false);
            }

            progress.Report(100);
        }
    }
}
