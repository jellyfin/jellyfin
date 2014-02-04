using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Movies;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.AdultVideos
{
    class AdultVideoXmlProvider : BaseXmlProvider, ILocalMetadataProvider<AdultVideo>
    {
        private readonly ILogger _logger;

        public AdultVideoXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<AdultVideo>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<AdultVideo>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                result.Item = new AdultVideo();

                new MovieXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
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
            return MovieXmlProvider.GetXmlFileInfo(path, FileSystem);
        }
    }
}
