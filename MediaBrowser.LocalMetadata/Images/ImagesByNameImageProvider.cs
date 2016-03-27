using System.Collections.Generic;
using System.IO;
using CommonIO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.LocalMetadata.Images
{
    public class ImagesByNameImageProvider : ILocalImageFileProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;

        public ImagesByNameImageProvider(IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _fileSystem = fileSystem;
            _config = config;
        }

        public string Name
        {
            get { return "Images By Name"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is CollectionFolder;
        }

        public int Order
        {
            get
            {
                // Run after LocalImageProvider, and after CollectionFolderImageProvider
                return 2;
            }
        }

        public List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService)
        {
            var name = _fileSystem.GetValidFilename(item.Name);

            var path = Path.Combine(_config.ApplicationPaths.GeneralPath, name);

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
