using System.Threading;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class PlaylistXmlProvider : BaseXmlProvider<Playlist>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public PlaylistXmlProvider(
            IFileSystem fileSystem,
            ILogger<PlaylistXmlProvider> logger,
            IProviderManager providerManager)
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
