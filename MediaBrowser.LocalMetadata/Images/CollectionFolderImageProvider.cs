using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.LocalMetadata.Images
{
    public class CollectionFolderLocalImageProvider : ILocalImageFileProvider, IHasOrder
    {
        public string Name
        {
            get { return "Collection Folder Images"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is CollectionFolder && item.SupportsLocalMetadata;
        }

        public int Order
        {
            get
            {
                // Run after LocalImageProvider
                return 1;
            }
        }

        public List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService)
        {
            var collectionFolder = (CollectionFolder)item;

            return new LocalImageProvider().GetImages(item, collectionFolder.PhysicalLocations, directoryService);
        }
    }
}
