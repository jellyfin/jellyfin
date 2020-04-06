using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    /// <summary>
    /// Nfo provider for music videos.
    /// </summary>
    public class MusicVideoNfoProvider : BaseVideoNfoProvider<MusicVideo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MusicVideoNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public MusicVideoNfoProvider(ILogger<MusicVideoNfoProvider> logger, IFileSystem fileSystem, IConfigurationManager config, IProviderManager providerManager)
            : base(logger, fileSystem, config, providerManager)
        {
        }
    }
}
