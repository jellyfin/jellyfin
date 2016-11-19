using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.Data
{
    public class CleanDatabaseScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public CleanDatabaseScheduledTask(ILibraryManager libraryManager, IItemRepository itemRepo, ILogger logger, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _logger = logger;
            _fileSystem = fileSystem;
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
            // Ensure these objects are lazy loaded.
            // Without this there is a deadlock that will need to be investigated
            var rootChildren = _libraryManager.RootFolder.Children.ToList();
            rootChildren = _libraryManager.GetUserRootFolder().Children.ToList();

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p =>
            {
                double newPercentCommplete = .45 * p;
                progress.Report(newPercentCommplete);
            });
            await CleanDeadItems(cancellationToken, innerProgress).ConfigureAwait(false);
            progress.Report(45);

            innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p =>
            {
                double newPercentCommplete = 45 + .55 * p;
                progress.Report(newPercentCommplete);
            });
            await CleanDeletedItems(cancellationToken, innerProgress).ConfigureAwait(false);
            progress.Report(100);

            await _itemRepo.UpdateInheritedValues(cancellationToken).ConfigureAwait(false);
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
                    _logger.Info("Cleaning item {0} type: {1} path: {2}", item.Name, item.GetType().Name, item.Path ?? string.Empty);

                    await item.Delete(new DeleteOptions
                    {
                        DeleteFileLocation = false

                    }).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numItems;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        private async Task CleanDeletedItems(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var result = _itemRepo.GetItemIdsWithPath(new InternalItemsQuery
            {
                LocationTypes = new[] { LocationType.FileSystem },
                //Limit = limit,

                // These have their own cleanup routines
                ExcludeItemTypes = new[]
                {
                    typeof(Person).Name,
                    typeof(Genre).Name,
                    typeof(MusicGenre).Name,
                    typeof(GameGenre).Name,
                    typeof(Studio).Name,
                    typeof(Year).Name,
                    typeof(Channel).Name,
                    typeof(AggregateFolder).Name,
                    typeof(CollectionFolder).Name
                }
            });

            var numComplete = 0;
            var numItems = result.Items.Length;

            foreach (var item in result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = item.Item2;

                try
                {
                    if (_fileSystem.FileExists(path) || _fileSystem.DirectoryExists(path))
                    {
                        continue;
                    }

                    var libraryItem = _libraryManager.GetItemById(item.Item1);

                    if (libraryItem.IsTopParent)
                    {
                        continue;
                    }

                    var hasDualAccess = libraryItem as IHasDualAccess;
                    if (hasDualAccess != null && hasDualAccess.IsAccessedByName)
                    {
                        continue;
                    }

                    var libraryItemPath = libraryItem.Path;
                    if (!string.Equals(libraryItemPath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Error("CleanDeletedItems aborting delete for item {0}-{1} because paths don't match. {2}---{3}", libraryItem.Id, libraryItem.Name, libraryItem.Path ?? string.Empty, path ?? string.Empty);
                        continue;
                    }

                    if (Folder.IsPathOffline(path))
                    {
                        await libraryItem.UpdateIsOffline(true).ConfigureAwait(false);
                        continue;
                    }

                    _logger.Info("Deleting item from database {0} because path no longer exists. type: {1} path: {2}", libraryItem.Name, libraryItem.GetType().Name, libraryItemPath ?? string.Empty);

                    await libraryItem.OnFileDeleted().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in CleanDeletedItems. File {0}", ex, path);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numItems;
                progress.Report(percent * 100);
            }
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] { 
            
                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks}
            };
        }

        public string Key
        {
            get { return "CleanDatabase"; }
        }
    }
}