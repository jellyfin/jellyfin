using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Globalization;

namespace Emby.Server.Implementations.Collections
{
    public class CollectionManager : ICollectionManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationManager _localizationManager;
        private IApplicationPaths _appPaths;

        public event EventHandler<CollectionCreatedEventArgs> CollectionCreated;
        public event EventHandler<CollectionModifiedEventArgs> ItemsAddedToCollection;
        public event EventHandler<CollectionModifiedEventArgs> ItemsRemovedFromCollection;

        public CollectionManager(ILibraryManager libraryManager, IApplicationPaths appPaths, ILocalizationManager localizationManager, IFileSystem fileSystem, ILibraryMonitor iLibraryMonitor, ILogger logger, IProviderManager providerManager)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
            _logger = logger;
            _providerManager = providerManager;
            _localizationManager = localizationManager;
            _appPaths = appPaths;
        }

        private IEnumerable<Folder> FindFolders(string path)
        {
            return _libraryManager
                .RootFolder
                .Children
                .OfType<Folder>()
                .Where(i => _fileSystem.AreEqual(path, i.Path) || _fileSystem.ContainsSubPath(i.Path, path));
        }

        internal async Task<Folder> EnsureLibraryFolder(string path, bool createIfNeeded)
        {
            var existingFolders = FindFolders(path)
                .ToList();

            if (existingFolders.Count > 0)
            {
                return existingFolders[0];
            }

            if (!createIfNeeded)
            {
                return null;
            }

            _fileSystem.CreateDirectory(path);

            var libraryOptions = new LibraryOptions
            {
                PathInfos = new[] { new MediaPathInfo { Path = path } },
                EnableRealtimeMonitor = false,
                SaveLocalMetadata = true
            };

            var name = _localizationManager.GetLocalizedString("Collections");

            await _libraryManager.AddVirtualFolder(name, CollectionType.BoxSets, libraryOptions, true).ConfigureAwait(false);

            return FindFolders(path).First();
        }

        internal string GetCollectionsFolderPath()
        {
            return Path.Combine(_appPaths.DataPath, "collections");
        }

        private Task<Folder> GetCollectionsFolder(bool createIfNeeded)
        {
            return EnsureLibraryFolder(GetCollectionsFolderPath(), createIfNeeded);
        }

        private IEnumerable<BoxSet> GetCollections(User user)
        {
            var folder = GetCollectionsFolder(false).Result;

            return folder == null ?
                new List<BoxSet>() :
                folder.GetChildren(user, true).OfType<BoxSet>();
        }

        public BoxSet CreateCollection(CollectionCreationOptions options)
        {
            var name = options.Name;

            // Need to use the [boxset] suffix
            // If internet metadata is not found, or if xml saving is off there will be no collection.xml
            // This could cause it to get re-resolved as a plain folder
            var folderName = _fileSystem.GetValidFilename(name) + " [boxset]";

            var parentFolder = GetCollectionsFolder(true).Result;

            if (parentFolder == null)
            {
                throw new ArgumentException();
            }

            var path = Path.Combine(parentFolder.Path, folderName);

            _iLibraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                _fileSystem.CreateDirectory(path);

                var collection = new BoxSet
                {
                    Name = name,
                    Path = path,
                    IsLocked = options.IsLocked,
                    ProviderIds = options.ProviderIds,
                    DateCreated = DateTime.UtcNow
                };

                parentFolder.AddChild(collection, CancellationToken.None);

                if (options.ItemIdList.Length > 0)
                {
                    AddToCollection(collection.Id, options.ItemIdList, false, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
                    {
                        // The initial adding of items is going to create a local metadata file
                        // This will cause internet metadata to be skipped as a result
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                    });
                }
                else
                {
                    _providerManager.QueueRefresh(collection.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), RefreshPriority.High);
                }

                CollectionCreated?.Invoke(this, new CollectionCreatedEventArgs
                {
                    Collection = collection,
                    Options = options
                });

                return collection;
            }
            finally
            {
                // Refresh handled internally
                _iLibraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        public void AddToCollection(Guid collectionId, IEnumerable<string> ids)
        {
            AddToCollection(collectionId, ids, true, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)));
        }

        public void AddToCollection(Guid collectionId, IEnumerable<Guid> ids)
        {
            AddToCollection(collectionId, ids.Select(i => i.ToString("N")), true, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)));
        }

        private void AddToCollection(Guid collectionId, IEnumerable<string> ids, bool fireEvent, MetadataRefreshOptions refreshOptions)
        {
            var collection = _libraryManager.GetItemById(collectionId) as BoxSet;

            if (collection == null)
            {
                throw new ArgumentException("No collection exists with the supplied Id");
            }

            var list = new List<LinkedChild>();
            var itemList = new List<BaseItem>();

            var linkedChildrenList = collection.GetLinkedChildren();
            var currentLinkedChildrenIds = linkedChildrenList.Select(i => i.Id).ToList();

            foreach (var id in ids)
            {
                var guidId = new Guid(id);
                var item = _libraryManager.GetItemById(guidId);

                if (item == null)
                {
                    throw new ArgumentException("No item exists with the supplied Id");
                }

                if (!currentLinkedChildrenIds.Contains(guidId))
                {
                    itemList.Add(item);

                    list.Add(LinkedChild.Create(item));
                    linkedChildrenList.Add(item);
                }
            }

            if (list.Count > 0)
            {
                var newList = collection.LinkedChildren.ToList();
                newList.AddRange(list);
                collection.LinkedChildren = newList.ToArray();

                collection.UpdateRatingToItems(linkedChildrenList);

                collection.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

                refreshOptions.ForceSave = true;
                _providerManager.QueueRefresh(collection.Id, refreshOptions, RefreshPriority.High);

                if (fireEvent)
                {
                    ItemsAddedToCollection?.Invoke(this, new CollectionModifiedEventArgs
                    {
                        Collection = collection,
                        ItemsChanged = itemList
                    });
                }
            }
        }

        public void RemoveFromCollection(Guid collectionId, IEnumerable<string> itemIds)
        {
            RemoveFromCollection(collectionId, itemIds.Select(i => new Guid(i)));
        }

        public void RemoveFromCollection(Guid collectionId, IEnumerable<Guid> itemIds)
        {
            var collection = _libraryManager.GetItemById(collectionId) as BoxSet;

            if (collection == null)
            {
                throw new ArgumentException("No collection exists with the supplied Id");
            }

            var list = new List<LinkedChild>();
            var itemList = new List<BaseItem>();

            foreach (var guidId in itemIds)
            {
                var childItem = _libraryManager.GetItemById(guidId);

                var child = collection.LinkedChildren.FirstOrDefault(i => (i.ItemId.HasValue && i.ItemId.Value == guidId) || (childItem != null && string.Equals(childItem.Path, i.Path, StringComparison.OrdinalIgnoreCase)));

                if (child == null)
                {
                    _logger.LogWarning("No collection title exists with the supplied Id");
                    continue;
                }

                list.Add(child);

                if (childItem != null)
                {
                    itemList.Add(childItem);
                }
            }

            if (list.Count > 0)
            {
                collection.LinkedChildren = collection.LinkedChildren.Except(list).ToArray();
            }

            collection.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
            _providerManager.QueueRefresh(collection.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
            {
                ForceSave = true
            }, RefreshPriority.High);

            ItemsRemovedFromCollection?.Invoke(this, new CollectionModifiedEventArgs
            {
                Collection = collection,
                ItemsChanged = itemList
            });
        }

        public IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user)
        {
            var results = new Dictionary<Guid, BaseItem>();

            var allBoxsets = GetCollections(user).ToList();

            foreach (var item in items)
            {
                var grouping = item as ISupportsBoxSetGrouping;

                if (grouping == null)
                {
                    results[item.Id] = item;
                }
                else
                {
                    var itemId = item.Id;

                    var currentBoxSets = allBoxsets
                        .Where(i => i.ContainsLinkedChildByItemId(itemId))
                        .ToList();

                    if (currentBoxSets.Count > 0)
                    {
                        foreach (var boxset in currentBoxSets)
                        {
                            results[boxset.Id] = boxset;
                        }
                    }
                    else
                    {
                        results[item.Id] = item;
                    }
                }
            }

            return results.Values;
        }
    }

    public class CollectionManagerEntryPoint : IServerEntryPoint
    {
        private readonly CollectionManager _collectionManager;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private ILogger _logger;

        public CollectionManagerEntryPoint(ICollectionManager collectionManager, IServerConfigurationManager config, IFileSystem fileSystem, ILogger logger)
        {
            _collectionManager = (CollectionManager)collectionManager;
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async void Run()
        {
            if (!_config.Configuration.CollectionsUpgraded && _config.Configuration.IsStartupWizardCompleted)
            {
                var path = _collectionManager.GetCollectionsFolderPath();

                if (_fileSystem.DirectoryExists(path))
                {
                    try
                    {
                        await _collectionManager.EnsureLibraryFolder(path, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating camera uploads library");
                    }

                    _config.Configuration.CollectionsUpgraded = true;
                    _config.SaveConfiguration();
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CollectionManagerEntryPoint() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
