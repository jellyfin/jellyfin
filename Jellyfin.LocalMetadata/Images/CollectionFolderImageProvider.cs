using System.Collections.Generic;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.IO;

namespace Jellyfin.LocalMetadata.Images
{
    public class CollectionFolderLocalImageProvider : ILocalImageFileProvider, IHasOrder
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
