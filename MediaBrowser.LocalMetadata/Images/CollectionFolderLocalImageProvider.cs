using System.Collections.Generic;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata.Images
{
    /// <summary>
    /// Collection folder local image provider.
    /// </summary>
    public class CollectionFolderLocalImageProvider : ILocalImageProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionFolderLocalImageProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="namingOptions">The library naming rules.</param>
        public CollectionFolderLocalImageProvider(IFileSystem fileSystem, NamingOptions namingOptions)
        {
            _fileSystem = fileSystem;
            _namingOptions = namingOptions;
        }

        /// <inheritdoc />
        public string Name => "Collection Folder Images";

        /// Run after LocalImageProvider
        /// <inheritdoc />
        public int Order => 1;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is CollectionFolder && item.SupportsLocalMetadata;
        }

        /// <inheritdoc />
        public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            var collectionFolder = (CollectionFolder)item;

            return new LocalImageProvider(_fileSystem, _namingOptions).GetImages(item, collectionFolder.PhysicalLocations, directoryService);
        }
    }
}
