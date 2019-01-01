using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Data
{
    public class CleanDatabaseScheduledTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _appPaths;

        public CleanDatabaseScheduledTask(ILibraryManager libraryManager, IItemRepository itemRepo, ILogger logger, IFileSystem fileSystem, IApplicationPaths appPaths)
        {
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _logger = logger;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            CleanDeadItems(cancellationToken, progress);
            return Task.CompletedTask;
        }

        private void CleanDeadItems(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                HasDeadParentId = true
            });

            var numComplete = 0;
            var numItems = itemIds.Count;

            _logger.LogDebug("Cleaning {0} items with dead parent links", numItems);

            foreach (var itemId in itemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = _libraryManager.GetItemById(itemId);

                if (item != null)
                {
                    _logger.LogInformation("Cleaning item {0} type: {1} path: {2}", item.Name, item.GetType().Name, item.Path ?? string.Empty);

                    _libraryManager.DeleteItem(item, new DeleteOptions
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
    }
}
