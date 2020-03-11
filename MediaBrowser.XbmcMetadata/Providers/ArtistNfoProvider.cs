using System.IO;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    /// <summary>
    /// Nfo provider for artists.
    /// </summary>
    public class ArtistNfoProvider : BaseNfoProvider<MusicArtist>
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public ArtistNfoProvider(
            IFileSystem fileSystem,
            ILogger<ArtistNfoProvider> logger,
            IConfigurationManager config,
            IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<MusicArtist> result, string path, CancellationToken cancellationToken)
        {
            new BaseNfoParser<MusicArtist>(_logger, _config, _providerManager).Fetch(result, path, cancellationToken);
        }

        /// <inheritdoc />
        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(Path.Combine(info.Path, "artist.nfo"));
    }
}
