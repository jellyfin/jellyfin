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
    public class AlbumNfoProvider : BaseNfoProvider<MusicAlbum>
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        public AlbumNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<MusicAlbum> result, string path, CancellationToken cancellationToken)
        {
            new BaseNfoParser<MusicAlbum>(_logger, _config, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "album.nfo"));
        }
    }
}
