using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class SeriesProviderFromXml
    /// </summary>
    public class SeriesXmlProvider : BaseXmlProvider, ILocalMetadataProvider<Series>
    {
        private readonly ILogger _logger;

        public SeriesXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<Series>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<Series>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var person = new Series();

                new SeriesXmlParser(_logger).Fetch(person, path, cancellationToken);
                result.HasMetadata = true;
                result.Item = person;
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
            return new FileInfo(Path.Combine(path, "series.xml"));
        }
    }
}
