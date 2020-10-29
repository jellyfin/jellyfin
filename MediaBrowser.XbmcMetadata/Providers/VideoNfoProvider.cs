using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    /// <summary>
    /// Nfo provider for videos.
    /// </summary>
    public class VideoNfoProvider : BaseVideoNfoProvider<Video>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="baseItemManager">The base item manager.</param>
        public VideoNfoProvider(ILogger<VideoNfoProvider> logger, IFileSystem fileSystem, IConfigurationManager config, IProviderManager providerManager, IBaseItemManager baseItemManager)
            : base(logger, fileSystem, config, providerManager, baseItemManager)
        {
        }
    }
}
