using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    class MusicGenresValidator
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        public MusicGenresValidator(ILibraryManager libraryManager, ILogger logger)
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
            var items = _libraryManager.RootFolder.GetRecursiveChildren(i => i is IHasMusicGenres)
                .SelectMany(i => i.Genres)
                .DistinctNames()
                .ToList();

            var numComplete = 0;
            var count = items.Count;

            var validIds = new List<Guid>();

            foreach (var name in items)
            {
                try
                {
                    var itemByName = _libraryManager.GetMusicGenre(name);

                    validIds.Add(itemByName.Id);

                    await itemByName.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Don't clutter the log
                    break;
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

            var allIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(MusicGenre).Name }
            });

            var invalidIds = allIds
                .Except(validIds)
                .ToList();

            foreach (var id in invalidIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var item = _libraryManager.GetItemById(id);

                await _libraryManager.DeleteItem(item, new DeleteOptions
                {
                    DeleteFileLocation = false

                }).ConfigureAwait(false);
            }

            progress.Report(100);
        }
    }
}
