using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Parsers;
using System.IO;
using System.Threading;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public class ArtistNfoProvider : BaseNfoProvider<MusicArtist>
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;

        public ArtistNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
        }

        protected override void Fetch(LocalMetadataResult<MusicArtist> result, string path, CancellationToken cancellationToken)
        {
            new BaseNfoParser<MusicArtist>(_logger, _config).Fetch(result.Item, result.UserDataLIst, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "artist.nfo"));
        }
    }
}
