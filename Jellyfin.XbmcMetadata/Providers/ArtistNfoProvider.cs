using System.IO;
using System.Threading;
using Jellyfin.Common.Configuration;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.IO;
using Jellyfin.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.XbmcMetadata.Providers
{
    public class ArtistNfoProvider : BaseNfoProvider<MusicArtist>
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        public ArtistNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<MusicArtist> result, string path, CancellationToken cancellationToken)
        {
            new BaseNfoParser<MusicArtist>(_logger, _config, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "artist.nfo"));
        }
    }
}
