using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
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
        private readonly ILogger<CollectionManager> _logger;
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
            _logger = loggerFactory.CreateLogger<CollectionManager>();
            _providerManager = providerManager;
            _localizationManager = localizationManager;
            _appPaths = appPaths;
        }

        /// <inheritdoc />
        public event EventHandler<CollectionCreatedEventArgs>? CollectionCreated;

        /// <inheritdoc />
        public event EventHandler<CollectionModifiedEventArgs>? ItemsAddedToCollection;

        /// <inheritdoc />
        public event EventHandler<CollectionModifiedEventArgs>? ItemsRemovedFromCollection;

        private IEnumerable<Folder> FindFolders(string path)
        {
            return _libraryManager
                .RootFolder
                .Children
                .OfType<Folder>()
                .Where(i => _fileSystem.AreEqual(path, i.Path) || _fileSystem.ContainsSubPath(i.Path, path));
        }

        internal async Task<Folder?> EnsureLibraryFolder(string path, bool createIfNeeded)
        {
            var existingFolder = FindFolders(path).FirstOrDefault();
            if (existingFolder is not null)
            {
                return existingFolder;
            }

            if (!createIfNeeded)
            {
                return null;
            }

            Directory.CreateDirectory(path);

            var libraryOptions = new LibraryOptions
            {
                PathInfos = new[] { new MediaPathInfo(path) },
                EnableRealtimeMonitor = false,
                SaveLocalMetadata = true
            };

            var name = _localizationManager.GetLocalizedString("Collections");

            await _libraryManager.AddVirtualFolder(name, CollectionTypeOptions.boxsets, libraryOptions, true).ConfigureAwait(false);

            return FindFolders(path).First();
        }

        internal string GetCollectionsFolderPath()
        {
            return Path.Combine(_appPaths.DataPath, "collections");
        }

        /// <inheritdoc />
        public Task<Folder?> GetCollectionsFolder(bool createIfNeeded)
        {
            return EnsureLibraryFolder(GetCollectionsFolderPath(), createIfNeeded);
        }

        private IEnumerable<BoxSet> GetCollections(User user)
        {
            var folder = GetCollectionsFolder(false).GetAwaiter().GetResult();

            return folder is null
                ? Enumerable.Empty<BoxSet>()
                : folder.GetChildren(user, true).OfType<BoxSet>();
        }

        /// <inheritdoc />
        public async Task<BoxSet> CreateCollectionAsync(CollectionCreationOptions options)
        {
            var name = options.Name;

            // Need to use the [boxset] suffix
            // If internet metadata is not found, or if xml saving is off there will be no collection.xml
            // This could cause it to get re-resolved as a plain folder
            var folderName = _fileSystem.GetValidFilename(name) + " [boxset]";

            var parentFolder = await GetCollectionsFolder(true).ConfigureAwait(false);

            if (parentFolder is null)
            {
                throw new ArgumentException(nameof(parentFolder));
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

                parentFolder.AddChild(collection);

                if (options.ItemIdList.Count > 0)
                {
                    await AddToCollectionAsync(
                        collection.Id,
                        options.ItemIdList.Select(x => new Guid(x)),
                        false,
                        new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                        {
                            // The initial adding of items is going to create a local metadata file
                            // This will cause internet metadata to be skipped as a result
                            MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                        }).ConfigureAwait(false);
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
        public Task AddToCollectionAsync(Guid collectionId, IEnumerable<Guid> itemIds)
            => AddToCollectionAsync(collectionId, itemIds, true, new MetadataRefreshOptions(new DirectoryService(_fileSystem)));

        private async Task AddToCollectionAsync(Guid collectionId, IEnumerable<Guid> ids, bool fireEvent, MetadataRefreshOptions refreshOptions)
        {
            if (_libraryManager.GetItemById(collectionId) is not BoxSet collection)
            {
                throw new ArgumentException("No collection exists with the supplied collectionId " + collectionId);
            }

            List<BaseItem>? itemList = null;

            var linkedChildrenList = collection.GetLinkedChildren();
            var currentLinkedChildrenIds = linkedChildrenList.Select(i => i.Id).ToList();

            foreach (var id in ids)
            {
                var item = _libraryManager.GetItemById(id);

                if (item is null)
                {
                    throw new ArgumentException("No item exists with the supplied Id " + id);
                }

                if (!currentLinkedChildrenIds.Contains(id))
                {
                    (itemList ??= new()).Add(item);

                    linkedChildrenList.Add(item);
                }
            }

            if (itemList is not null)
            {
                var originalLen = collection.LinkedChildren.Length;
                var newItemCount = itemList.Count;
                LinkedChild[] newChildren = new LinkedChild[originalLen + newItemCount];
                collection.LinkedChildren.CopyTo(newChildren, 0);
                for (int i = 0; i < newItemCount; i++)
                {
                    newChildren[originalLen + i] = LinkedChild.Create(itemList[i]);
                }

                collection.LinkedChildren = newChildren;
                collection.UpdateRatingToItems(linkedChildrenList);

                await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

                refreshOptions.ForceSave = true;
                _providerManager.QueueRefresh(collection.Id, refreshOptions, RefreshPriority.High);

                if (fireEvent)
                {
                    ItemsAddedToCollection?.Invoke(this, new CollectionModifiedEventArgs(collection, itemList));
                }
            }
        }

        /// <inheritdoc />
        public async Task RemoveFromCollectionAsync(Guid collectionId, IEnumerable<Guid> itemIds)
        {
            if (_libraryManager.GetItemById(collectionId) is not BoxSet collection)
            {
                throw new ArgumentException("No collection exists with the supplied Id");
            }

            var list = new List<LinkedChild>();
            var itemList = new List<BaseItem>();

            foreach (var guidId in itemIds)
            {
                var childItem = _libraryManager.GetItemById(guidId);

                var child = collection.LinkedChildren.FirstOrDefault(i => (i.ItemId.HasValue && i.ItemId.Value.Equals(guidId)) || (childItem is not null && string.Equals(childItem.Path, i.Path, StringComparison.OrdinalIgnoreCase)));

                if (child is null)
                {
                    _logger.LogWarning("No collection title exists with the supplied Id");
                    continue;
                }

                list.Add(child);

                if (childItem is not null)
                {
                    itemList.Add(childItem);
                }
            }

            if (list.Count > 0)
            {
                collection.LinkedChildren = collection.LinkedChildren.Except(list).ToArray();
            }

            await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            _providerManager.QueueRefresh(
                collection.Id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ForceSave = true
                },
                RefreshPriority.High);

            ItemsRemovedFromCollection?.Invoke(this, new CollectionModifiedEventArgs(collection, itemList));
        }

        /// <inheritdoc />
        public IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user)
        {
            var results = new Dictionary<Guid, BaseItem>();

            var allBoxSets = GetCollections(user).ToList();

            foreach (var item in items)
            {
                if (item is ISupportsBoxSetGrouping)
                {
                    var itemId = item.Id;

                    var itemIsInBoxSet = false;
                    foreach (var boxSet in allBoxSets)
                    {
                        if (!boxSet.ContainsLinkedChildByItemId(itemId))
                        {
                            continue;
                        }

                        itemIsInBoxSet = true;

                        results.TryAdd(boxSet.Id, boxSet);
                    }

                    // skip any item that is in a box set
                    if (itemIsInBoxSet)
                    {
                        continue;
                    }

                    var alreadyInResults = false;

                    // this is kind of a performance hack because only Video has alternate versions that should be in a box set?
                    if (item is Video video)
                    {
                        foreach (var childId in video.GetLocalAlternateVersionIds())
                        {
                            if (!results.ContainsKey(childId))
                            {
                                continue;
                            }

                            alreadyInResults = true;
                            break;
                        }
                    }

                    if (alreadyInResults)
                    {
                        continue;
                    }
                }

                results[item.Id] = item;
            }

            return results.Values;
        }
    }
}
