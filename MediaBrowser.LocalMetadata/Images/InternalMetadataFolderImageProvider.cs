using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.IO;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Images
{
    public class InternalMetadataFolderImageProvider : ILocalImageFileProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public InternalMetadataFolderImageProvider(IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _config = config;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "Internal Images"; }
        }

        public bool Supports(IHasImages item)
        {
            if (item is Photo)
            {
                return false;
            }

            if (!item.IsSaveLocalMetadataEnabled())
            {
                return true;
            }

            // Extracted images will be saved in here
            if (item is Audio)
            {
                return true;
            }

            if (item.SupportsLocalMetadata && !item.AlwaysScanInternalMetadataPath)
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

        public List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService)
        {
            var path = item.GetInternalMetadataPath();

            try
            {
                return new LocalImageProvider(_fileSystem).GetImages(item, path, directoryService);
            }
            catch (DirectoryNotFoundException)
            {
                return new List<LocalImageInfo>();
            }
        }
    }
}
