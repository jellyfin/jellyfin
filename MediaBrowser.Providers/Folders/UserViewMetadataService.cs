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
    public class UserViewMetadataService : MetadataService<UserView, ItemLookupInfo>
    {
        public UserViewMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<UserViewMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }
    }
}
