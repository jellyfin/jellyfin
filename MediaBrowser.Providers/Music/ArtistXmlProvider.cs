using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Music
{
    class ArtistXmlProvider : BaseXmlProvider<MusicArtist>
    {
        private readonly ILogger _logger;

        public ArtistXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(MusicArtist item, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<MusicArtist>(_logger).Fetch(item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info)
        {
            return new FileInfo(Path.Combine(info.Path, "artist.xml"));
        }
    }
}
