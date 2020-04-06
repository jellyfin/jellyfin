using System.Collections.Generic;
using System.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Images
{
    public class InternalMetadataFolderImageProvider : ILocalImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public InternalMetadataFolderImageProvider(
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            ILogger<InternalMetadataFolderImageProvider> logger)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string Name => "Internal Images";

        public bool Supports(BaseItem item)
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
        // Make sure this is last so that all other locations are scanned first
        public int Order => 1000;

        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            var path = item.GetInternalMetadataPath();

            if (!Directory.Exists(path))
            {
                return new List<LocalImageInfo>();
            }

            try
            {
                return new LocalImageProvider(_fileSystem).GetImages(item, path, false, directoryService);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error while getting images for {Library}", item.Name);
                return new List<LocalImageInfo>();
            }
        }
    }
}
