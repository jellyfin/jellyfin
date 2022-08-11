#pragma warning disable CS1591

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Folders
{
    public class CollectionFolderMetadataService : MetadataService<CollectionFolder, ItemLookupInfo>
    {
        public CollectionFolderMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<CollectionFolderMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }
    }
}
