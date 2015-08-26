using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.Persistence
{
    class CleanDatabaseScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;
        private readonly ILogger _logger;

        public CleanDatabaseScheduledTask(ILibraryManager libraryManager, IItemRepository itemRepo, ILogger logger)
        {
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _logger = logger;
        }

        public string Name
        {
            get { return "Clean Database"; }
        }

        public string Description
        {
            get { return "Deletes obsolete content from the database."; }
        }

        public string Category
        {
            get { return "Library"; }
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(progress.Report);

            await UpdateToLatestSchema(cancellationToken, innerProgress).ConfigureAwait(false);
        }

        private async Task UpdateToLatestSchema(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                IsCurrentSchema = false,
                Limit = 50000
            });

            var numComplete = 0;
            var numItems = itemIds.Count;

            _logger.Debug("Upgrading schema for {0} items", numItems);

            foreach (var itemId in itemIds)
            {
                var item = _libraryManager.GetItemById(itemId);

                if (item != null)
                {
                    await _itemRepo.SaveItem(item, cancellationToken).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numItems;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[] 
            { 
                new IntervalTrigger{ Interval = TimeSpan.FromDays(1)}
            };
        }
    }
}
