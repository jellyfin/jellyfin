using System.Threading;
using Jellyfin.Controller.Playlists;
using Jellyfin.Controller.Providers;
using Jellyfin.LocalMetadata.Parsers;
using Jellyfin.LocalMetadata.Savers;
using Jellyfin.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LocalMetadata.Providers
{
    public class PlaylistXmlProvider : BaseXmlProvider<Playlist>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public PlaylistXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<Playlist> result, string path, CancellationToken cancellationToken)
        {
            new PlaylistXmlParser(_logger, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(PlaylistXmlSaver.GetSavePath(info.Path, FileSystem));
        }
    }
}
