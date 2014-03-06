using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System;
using System.IO;
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

            var parentFolder = _libraryManager.GetItemById(options.ParentId) as Folder;

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

        public Task AddToCollection(Guid collectionId, Guid itemId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveFromCollection(Guid collectionId, Guid itemId)
        {
            throw new NotImplementedException();
        }
    }
}
