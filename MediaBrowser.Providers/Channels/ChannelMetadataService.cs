#pragma warning disable CS1591

using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Channels
{
    public class ChannelMetadataService : MetadataService<Channel, ItemLookupInfo>
    {
        public ChannelMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<ChannelMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }
    }
}
