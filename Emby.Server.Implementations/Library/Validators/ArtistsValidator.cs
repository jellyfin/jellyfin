using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Persistence;

namespace Emby.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class ArtistsValidator
    /// </summary>
    public class ArtistsValidator
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsPostScanTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
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
                    _logger.ErrorException("Error refreshing {0}", ex, name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;

                progress.Report(percent);
            }

            names = names.Select(i => i.RemoveDiacritics()).DistinctNames().ToList();

            var artistEntities = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(MusicArtist).Name }

            }).Cast<MusicArtist>().ToList();

            foreach (var artist in artistEntities)
            {
                if (!artist.IsAccessedByName)
                {
                    continue;
                }

                var name = (artist.Name ?? string.Empty).RemoveDiacritics();

                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.Info("Deleting dead artist {0} {1}.", artist.Id.ToString("N"), artist.Name);

                    await _libraryManager.DeleteItem(artist, new DeleteOptions
                    {
                        DeleteFileLocation = false

                    }).ConfigureAwait(false);
                }
            }

            progress.Report(100);
        }
    }
}
