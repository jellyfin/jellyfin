using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Collections
{
    /// <summary>
    /// The collection manager.
    /// </summary>
    public class CollectionManager : ICollectionManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionManager"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="iLibraryMonitor">The library monitor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="providerManager">The provider manager.</param>
        public CollectionManager(
            ILibraryManager libraryManager,
            IApplicationPaths appPaths,
            ILocalizationManager localizationManager,
            IFileSystem fileSystem,
            ILibraryMonitor iLibraryMonitor,
            ILoggerFactory loggerFactory,
            IProviderManager providerManager)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
            _logger = loggerFactory.CreateLogger(nameof(CollectionManager));
            _providerManager = providerManager;
            _localizationManager = localizationManager;
            _appPaths = appPaths;
        }

        /// <inheritdoc />
        public event EventHandler<CollectionCreatedEventArgs> CollectionCreated;

        /// <inheritdoc />
        public event EventHandler<CollectionModifiedEventArgs> ItemsAddedToCollection;

        /// <inheritdoc />
        public event EventHandler<CollectionModifiedEventArgs> ItemsRemovedFromCollection;

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

            Directory.CreateDirectory(path);

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

            return folder == null
                ? Enumerable.Empty<BoxSet>()
                : folder.GetChildren(user, true).OfType<BoxSet>();
        }

        /// <inheritdoc />
        public BoxSet CreateCollection(CollectionCreationOptions options)
        {
            var name = options.Name;

            // Need to use the [boxset] suffix
            // If internet metadata is not found, or if xml saving is off there will be no collection.xml
            // This could cause it to get re-resolved as a plain folder
            var folderName = _fileSystem.GetValidFilename(name) + " [boxset]";

            var parentFolder = GetCollectionsFolder(true).GetAwaiter().GetResult();

            if (parentFolder == null)
            {
                throw new ArgumentException();
            }

            var path = Path.Combine(parentFolder.Path, folderName);

            _iLibraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                Directory.CreateDirectory(path);

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
                    AddToCollection(collection.Id, options.ItemIdList, false, new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        // The initial adding of items is going to create a local metadata file
                        // This will cause internet metadata to be skipped as a result
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                    });
                }
                else
                {
                    _providerManager.QueueRefresh(collection.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);
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

        /// <inheritdoc />
        public void AddToCollection(Guid collectionId, IEnumerable<string> ids)
        {
            AddToCollection(collectionId, ids, true, new MetadataRefreshOptions(new DirectoryService(_fileSystem)));
        }

        /// <inheritdoc />
        public void AddToCollection(Guid collectionId, IEnumerable<Guid> ids)
        {
            AddToCollection(collectionId, ids.Select(i => i.ToString("N", CultureInfo.InvariantCulture)), true, new MetadataRefreshOptions(new DirectoryService(_fileSystem)));
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

        /// <inheritdoc />
        public void RemoveFromCollection(Guid collectionId, IEnumerable<string> itemIds)
        {
            RemoveFromCollection(collectionId, itemIds.Select(i => new Guid(i)));
        }

        /// <inheritdoc />
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
            _providerManager.QueueRefresh(
                collection.Id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ForceSave = true
                },
                RefreshPriority.High);

            ItemsRemovedFromCollection?.Invoke(this, new CollectionModifiedEventArgs
            {
                Collection = collection,
                ItemsChanged = itemList
            });
        }

        /// <inheritdoc />
        public IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user)
        {
            var results = new Dictionary<Guid, BaseItem>();

            var allBoxsets = GetCollections(user).ToList();

            foreach (var item in items)
            {
                if (!(item is ISupportsBoxSetGrouping))
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

    /// <summary>
    /// The collection manager entry point.
    /// </summary>
    public sealed class CollectionManagerEntryPoint : IServerEntryPoint
    {
        private readonly CollectionManager _collectionManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionManagerEntryPoint"/> class.
        /// </summary>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="config">The server configuration manager.</param>
        /// <param name="logger">The logger.</param>
        public CollectionManagerEntryPoint(
            ICollectionManager collectionManager,
            IServerConfigurationManager config,
            ILogger<CollectionManagerEntryPoint> logger)
        {
            _collectionManager = (CollectionManager)collectionManager;
            _config = config;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task RunAsync()
        {
            if (!_config.Configuration.CollectionsUpgraded && _config.Configuration.IsStartupWizardCompleted)
            {
                var path = _collectionManager.GetCollectionsFolderPath();

                if (Directory.Exists(path))
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

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
