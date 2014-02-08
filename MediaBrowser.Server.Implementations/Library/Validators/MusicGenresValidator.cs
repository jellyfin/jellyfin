using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
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
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        public MusicGenresValidator(ILibraryManager libraryManager, IUserManager userManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
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
            var userLibraries = _userManager.Users
                .Select(i => new Tuple<Guid, IList<BaseItem>>(i.Id, i.RootFolder.GetRecursiveChildren(i, m => m is IHasMusicGenres)))
                .ToList();

            var masterDictionary = new Dictionary<string, Dictionary<Guid, Dictionary<CountType, int>>>(StringComparer.OrdinalIgnoreCase);

            progress.Report(2);

            var numComplete = 0;

            foreach (var lib in userLibraries)
            {
                SetItemCounts(lib.Item1, lib.Item2, masterDictionary);

                numComplete++;
                double percent = numComplete;
                percent /= userLibraries.Count;
                percent *= 8;

                progress.Report(percent);
            }

            progress.Report(10);

            var count = masterDictionary.Count;
            numComplete = 0;

            foreach (var name in masterDictionary.Keys)
            {
                try
                {
                    await UpdateItemByNameCounts(name, cancellationToken, masterDictionary[name]).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Don't clutter the log
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error updating counts for {0}", ex, name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 90;

                progress.Report(percent + 10);
            }

            progress.Report(100);
        }

        private Task UpdateItemByNameCounts(string name, CancellationToken cancellationToken, Dictionary<Guid, Dictionary<CountType, int>> counts)
        {
            var itemByName = _libraryManager.GetMusicGenre(name);

            foreach (var libraryId in counts.Keys)
            {
                var itemCounts = CountHelpers.GetCounts(counts[libraryId]);

                itemByName.SetItemByNameCounts(libraryId, itemCounts);
            }

            return itemByName.RefreshMetadata(cancellationToken);
        }

        private void SetItemCounts(Guid userId, IEnumerable<BaseItem> allItems, Dictionary<string, Dictionary<Guid, Dictionary<CountType, int>>> masterDictionary)
        {
            foreach (var media in allItems)
            {
                var names = media
                    .Genres
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                CountHelpers.SetItemCounts(userId, media, names, masterDictionary);
            }
        }
    }
}
