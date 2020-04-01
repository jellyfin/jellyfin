using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata.Images
{
    public class CollectionFolderLocalImageProvider : ILocalImageProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;

        public CollectionFolderLocalImageProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Name => "Collection Folder Images";

        public bool Supports(BaseItem item)
        {
            return item is CollectionFolder && item.SupportsLocalMetadata;
        }

        // Run after LocalImageProvider
        public int Order => 1;

        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            var collectionFolder = (CollectionFolder)item;

            return new LocalImageProvider(_fileSystem).GetImages(item, collectionFolder.PhysicalLocations, true, directoryService);
        }
    }
}
