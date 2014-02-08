using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Providers.All
{
    public class InternalMetadataFolderImageProvider : ILocalImageFileProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;

        public InternalMetadataFolderImageProvider(IServerConfigurationManager config)
        {
            _config = config;
        }

        public string Name
        {
            get { return "Internal Images"; }
        }

        public bool Supports(IHasImages item)
        {
            if (!item.IsSaveLocalMetadataEnabled())
            {
                return true;
            }

            var locationType = item.LocationType;

            if (locationType == LocationType.FileSystem ||
                locationType == LocationType.Offline)
            {
                return false;
            }

            // These always save locally
            if (item is IItemByName || item is User)
            {
                return false;
            }
            
            return true;
        }

        public int Order
        {
            get
            {
                // Make sure this is last so that all other locations are scanned first
                return 1000;
            }
        }

        public List<LocalImageInfo> GetImages(IHasImages item, DirectoryService directoryService)
        {
            var path = _config.ApplicationPaths.GetInternalMetadataPath(item.Id);

            try
            {
                return new LocalImageProvider().GetImages(item, path, directoryService);
            }
            catch (DirectoryNotFoundException)
            {
                return new List<LocalImageInfo>();
            }
        }
    }
}
