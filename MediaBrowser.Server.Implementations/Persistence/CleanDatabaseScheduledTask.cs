using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Server.Implementations.ScheduledTasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class CleanDatabaseScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpServer _httpServer;
        private readonly ILocalizationManager _localization;
        private readonly ITaskManager _taskManager;

        public const int MigrationVersion = 23;
        public static bool EnableUnavailableMessage = false;

        public CleanDatabaseScheduledTask(ILibraryManager libraryManager, IItemRepository itemRepo, ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem, IHttpServer httpServer, ILocalizationManager localization, ITaskManager taskManager)
        {
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
            _httpServer = httpServer;
            _localization = localization;
            _taskManager = taskManager;
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
            OnProgress(0);

            // Ensure these objects are lazy loaded.
            // Without this there is a deadlock that will need to be investigated
            var rootChildren = _libraryManager.RootFolder.Children.ToList();
            rootChildren = _libraryManager.GetUserRootFolder().Children.ToList();

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p =>
            {
                double newPercentCommplete = .4 * p;
                OnProgress(newPercentCommplete);

                progress.Report(newPercentCommplete);
            });

            await UpdateToLatestSchema(cancellationToken, innerProgress).ConfigureAwait(false);

            innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p =>
            {
                double newPercentCommplete = 40 + .05 * p;
                OnProgress(newPercentCommplete);
                progress.Report(newPercentCommplete);
            });
            await CleanDeadItems(cancellationToken, innerProgress).ConfigureAwait(false);
            progress.Report(45);

            innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p =>
            {
                double newPercentCommplete = 45 + .55 * p;
                OnProgress(newPercentCommplete);
                progress.Report(newPercentCommplete);
            });
            await CleanDeletedItems(cancellationToken, innerProgress).ConfigureAwait(false);
            progress.Report(100);

            await _itemRepo.UpdateInheritedValues(cancellationToken).ConfigureAwait(false);

            if (_config.Configuration.MigrationVersion < MigrationVersion)
            {
                _config.Configuration.MigrationVersion = MigrationVersion;
                _config.SaveConfiguration();
            }

            if (_config.Configuration.SchemaVersion < SqliteItemRepository.LatestSchemaVersion)
            {
                _config.Configuration.SchemaVersion = SqliteItemRepository.LatestSchemaVersion;
                _config.SaveConfiguration();
            }

            if (EnableUnavailableMessage)
            {
                EnableUnavailableMessage = false;
                _httpServer.GlobalResponse = null;
                _taskManager.QueueScheduledTask<RefreshMediaLibraryTask>();
            }

            _taskManager.SuspendTriggers = false;
        }

        private void OnProgress(double newPercentCommplete)
        {
            if (EnableUnavailableMessage)
            {
                var html = "<!doctype html><html><head><title>Emby</title></head><body>";
                var text = _localization.GetLocalizedString("DbUpgradeMessage");
                html += string.Format(text, newPercentCommplete.ToString("N2", CultureInfo.InvariantCulture));

                html += "<script>setTimeout(function(){window.location.reload(true);}, 5000);</script>";
                html += "</body></html>";

                _httpServer.GlobalResponse = html;
            }
        }

        private async Task UpdateToLatestSchema(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                IsCurrentSchema = false,
                ExcludeItemTypes = new[] { typeof(LiveTvProgram).Name }
            });

            var numComplete = 0;
            var numItems = itemIds.Count;

            _logger.Debug("Upgrading schema for {0} items", numItems);

            var list = new List<BaseItem>();

            foreach (var itemId in itemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (itemId != Guid.Empty)
                {
                    // Somehow some invalid data got into the db. It probably predates the boundary checking
                    var item = _libraryManager.GetItemById(itemId);

                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                if (list.Count >= 1000)
                {
                    try
                    {
                        await _itemRepo.SaveItems(list, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error saving item", ex);
                    }

                    list.Clear();
                }

                numComplete++;
                double percent = numComplete;
                percent /= numItems;
                progress.Report(percent * 100);
            }

            if (list.Count > 0)
            {
                try
                {
                    await _itemRepo.SaveItems(list, cancellationToken).ConfigureAwait(false);
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

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
            {
                new IntervalTrigger{ Interval = TimeSpan.FromHours(24)}
            };
        }
    }
}