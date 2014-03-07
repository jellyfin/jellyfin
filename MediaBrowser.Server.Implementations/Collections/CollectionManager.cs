using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Collections
{
    public class CollectionManager : ICollectionManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;

        public CollectionManager(ILibraryManager libraryManager, IFileSystem fileSystem, ILibraryMonitor iLibraryMonitor)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
        }

        public async Task CreateCollection(CollectionCreationOptions options)
        {
            var name = options.Name;

            var folderName = _fileSystem.GetValidFilename(name);

            var parentFolder = GetParentFolder(options.ParentId);

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
                    Parent = parentFolder,
                    DisplayMediaType = "Collection",
                    Path = path,
                    DontFetchMeta = options.IsLocked
                };

                await parentFolder.AddChild(collection, CancellationToken.None).ConfigureAwait(false);

                await collection.RefreshMetadata(new MetadataRefreshOptions(), CancellationToken.None)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Refresh handled internally
                _iLibraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        private Folder GetParentFolder(Guid? parentId)
        {
            if (parentId.HasValue)
            {
                if (parentId.Value == Guid.Empty)
                {
                    throw new ArgumentNullException("parentId");
                }

                return _libraryManager.GetItemById(parentId.Value) as Folder;
            }

            return _libraryManager.RootFolder.Children.OfType<ManualCollectionsFolder>().FirstOrDefault() ??
                _libraryManager.RootFolder.GetHiddenChildren().OfType<ManualCollectionsFolder>().FirstOrDefault();
        }

        public async Task AddToCollection(Guid collectionId, IEnumerable<Guid> ids)
        {
            var collection = _libraryManager.GetItemById(collectionId) as BoxSet;

            if (collection == null)
            {
                throw new ArgumentException("No collection exists with the supplied Id");
            }

            var list = new List<LinkedChild>();

            foreach (var itemId in ids)
            {
                var item = _libraryManager.GetItemById(itemId);

                if (item == null)
                {
                    throw new ArgumentException("No item exists with the supplied Id");
                }

                if (collection.LinkedChildren.Any(i => i.ItemId.HasValue && i.ItemId == itemId))
                {
                    throw new ArgumentException("Item already exists in collection");
                }

                list.Add(new LinkedChild
                {
                    ItemName = item.Name,
                    ItemYear = item.ProductionYear,
                    ItemType = item.GetType().Name,
                    Type = LinkedChildType.Manual
                });
            }

            collection.LinkedChildren.AddRange(list);

            await collection.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            await collection.RefreshMetadata(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task RemoveFromCollection(Guid collectionId, IEnumerable<Guid> itemIds)
        {
            var collection = _libraryManager.GetItemById(collectionId) as BoxSet;

            if (collection == null)
            {
                throw new ArgumentException("No collection exists with the supplied Id");
            }

            var list = new List<LinkedChild>();

            foreach (var itemId in itemIds)
            {
                var child = collection.LinkedChildren.FirstOrDefault(i => i.ItemId.HasValue && i.ItemId.Value == itemId);

                if (child == null)
                {
                    throw new ArgumentException("No collection title exists with the supplied Id");
                }

                list.Add(child);
            }

            foreach (var child in list)
            {
                collection.LinkedChildren.Remove(child);
            }

            await collection.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            await collection.RefreshMetadata(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
