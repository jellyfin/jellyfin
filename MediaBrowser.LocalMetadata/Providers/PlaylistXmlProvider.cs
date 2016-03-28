using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Providers
{
    class PlaylistXmlProvider : BaseXmlProvider<Playlist>
    {
        private readonly ILogger _logger;

        public PlaylistXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(MetadataResult<Playlist> result, string path, CancellationToken cancellationToken)
        {
            new PlaylistXmlParser(_logger).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "playlist.xml"));
        }
    }
}
