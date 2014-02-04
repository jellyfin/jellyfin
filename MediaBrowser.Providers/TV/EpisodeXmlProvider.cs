using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class EpisodeXmlProvider : BaseXmlProvider, ILocalMetadataProvider<Episode>
    {
        private readonly ILogger _logger;

        public EpisodeXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<Episode>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<Episode>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                result.Item = new Episode();

                new EpisodeXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
                result.HasMetadata = true;
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

        protected override FileInfo GetXmlFile(string path)
        {
            var metadataPath = Path.GetDirectoryName(path);
            metadataPath = Path.Combine(metadataPath, "metadata");
            var metadataFile = Path.Combine(metadataPath, Path.ChangeExtension(Path.GetFileName(path), ".xml"));

            return new FileInfo(metadataFile);
        }
    }
}
