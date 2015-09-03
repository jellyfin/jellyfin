using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    class CleanDatabaseScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public CleanDatabaseScheduledTask(ILibraryManager libraryManager, IItemRepository itemRepo, ILogger logger, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _logger = logger;
            _config = config;
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
            innerProgress.RegisterAction(p => progress.Report(.95 * p));

            await UpdateToLatestSchema(cancellationToken, innerProgress).ConfigureAwait(false);

            innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(95 + (.05 * p)));

            await CleanDeadItems(cancellationToken, innerProgress).ConfigureAwait(false);

            progress.Report(100);
        }

        private async Task UpdateToLatestSchema(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                IsCurrentSchema = false,

                // These are constantly getting regenerated so don't bother with them here
                ExcludeItemTypes = new[] { typeof(LiveTvProgram).Name }
            });

            var numComplete = 0;
            var numItems = itemIds.Count;

            _logger.Debug("Upgrading schema for {0} items", numItems);

            foreach (var itemId in itemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (itemId == Guid.Empty)
                {
                    // Somehow some invalid data got into the db. It probably predates the boundary checking
                    continue;
                }

                var item = _libraryManager.GetItemById(itemId);

                if (item != null)
                {
                    try
                    {
                        await _itemRepo.SaveItem(item, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error saving item", ex);
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= numItems;
                progress.Report(percent * 100);
            }

            if (!_config.Configuration.DisableStartupScan)
            {
                _config.Configuration.DisableStartupScan = true;
                _config.SaveConfiguration();
            }

            progress.Report(100);
        }

        private async Task CleanDeadItems(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                HasDeadParentId = true
            });

            var numComplete = 0;
            var numItems = itemIds.Count;

            _logger.Debug("Cleaning {0} items with dead parent links", numItems);

            foreach (var itemId in itemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = _libraryManager.GetItemById(itemId);

                if (item != null)
                {
                    _logger.Debug("Cleaning item {0} type: {1} path: {2}", item.Name, item.GetType().Name, item.Path ?? string.Empty);

                    await _libraryManager.DeleteItem(item, new DeleteOptions
                    {
                        DeleteFileLocation = false
                    });
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
