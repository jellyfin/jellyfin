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
    class PeoplePostScanTask : ILibraryPostScanTask
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

        public PeoplePostScanTask(ILibraryManager libraryManager, IUserManager userManager, ILogger logger)
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
            var allItems = _libraryManager.RootFolder.RecursiveChildren.ToList();

            var userLibraries = _userManager.Users
                .Select(i => new Tuple<Guid, List<BaseItem>>(i.Id, i.RootFolder.GetRecursiveChildren(i).ToList()))
                .ToList();

            var allLibraryItems = allItems;

            var masterDictionary = new Dictionary<string, Dictionary<Guid, Dictionary<string, int>>>(StringComparer.OrdinalIgnoreCase);

            // Populate counts of items
            SetItemCounts(null, allLibraryItems, masterDictionary);

            progress.Report(2);

            var numComplete = 0;

            foreach (var lib in userLibraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    await UpdateItemByNameCounts(name, masterDictionary[name]).ConfigureAwait(false);
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

        private async Task UpdateItemByNameCounts(string name, Dictionary<Guid, Dictionary<string, int>> counts)
        {
            var itemByName = await _libraryManager.GetPerson(name).ConfigureAwait(false);

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
                    .People.Select(i => i.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                CountHelpers.SetItemCounts(userId, media, names, masterDictionary);
            }
        }

    }
}
