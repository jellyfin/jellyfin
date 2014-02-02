using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.BoxSets
{
    /// <summary>
    /// Class SeriesProviderFromXml
    /// </summary>
    public class BoxSetXmlProvider : BaseXmlProvider, ILocalMetadataProvider<BoxSet>
    {
        private readonly ILogger _logger;

        public BoxSetXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<BoxSet>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<BoxSet>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var item = new BoxSet();

                new BaseItemXmlParser<BoxSet>(_logger).Fetch(item, path, cancellationToken);
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
            get { return "Media Browser xml"; }
        }

        protected override FileInfo GetXmlFile(string path)
        {
            return new FileInfo(Path.Combine(path, "collection.xml"));
        }
    }
}
