using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    class ArtistXmlProvider : BaseXmlProvider, ILocalMetadataProvider<MusicArtist>
    {
        private readonly ILogger _logger;

        public ArtistXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<MusicArtist>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlPath(path);

            var result = new MetadataResult<MusicArtist>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var item = new MusicArtist();

                new BaseItemXmlParser<MusicArtist>(_logger).Fetch(item, path, cancellationToken);
                result.HasMetadata = true;
                result.Item = item;
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }
            finally
            {
                XmlParsingResourcePool.Release();
            }

            return result;
        }

        public string Name
        {
            get { return "Media Browser Xml"; }
        }

        protected override string GetXmlPath(string path)
        {
            return Path.Combine(path, "artist.xml");
        }
    }
}
