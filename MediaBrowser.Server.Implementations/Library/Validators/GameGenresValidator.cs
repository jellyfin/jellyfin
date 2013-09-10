using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    class GameGenresValidator
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly LibraryManager _libraryManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        public GameGenresValidator(LibraryManager libraryManager, IUserManager userManager, ILogger logger)
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
            var allItems = _libraryManager.RootFolder.RecursiveChildren.OfType<Game>().ToList();

            var userLibraries = _userManager.Users
                .Select(i => new Tuple<Guid, List<Game>>(i.Id, i.RootFolder.GetRecursiveChildren(i).OfType<Game>().ToList()))
                .ToList();

            var allLibraryItems = allItems;

            var masterDictionary = new Dictionary<string, Dictionary<Guid, Dictionary<string, int>>>(StringComparer.OrdinalIgnoreCase);

            // Populate counts of items
            SetItemCounts(null, allLibraryItems, masterDictionary);

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

            var names = masterDictionary.Keys.ToList();
            numComplete = 0;

            foreach (var name in names)
            {
                try
                {
                    await UpdateItemByNameCounts(name, cancellationToken, masterDictionary[name]).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error updating counts for {0}", ex, name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= names.Count;
                percent *= 90;

                progress.Report(percent + 10);
            }

            progress.Report(100);
        }

        private async Task UpdateItemByNameCounts(string name, CancellationToken cancellationToken, Dictionary<Guid, Dictionary<string, int>> counts)
        {
            var itemByName = await _libraryManager.GetGameGenre(name, cancellationToken, true, true).ConfigureAwait(false);

            foreach (var libraryId in counts.Keys.ToList())
            {
                var itemCounts = CountHelpers.GetCounts(counts[libraryId]);

                if (libraryId == Guid.Empty)
                {
                    itemByName.ItemCounts = itemCounts;
                }
                else
                {
                    itemByName.UserItemCounts[libraryId] = itemCounts;
                }
            }
        }

        private void SetItemCounts(Guid? userId, IEnumerable<BaseItem> allItems, Dictionary<string, Dictionary<Guid, Dictionary<string, int>>> masterDictionary)
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
