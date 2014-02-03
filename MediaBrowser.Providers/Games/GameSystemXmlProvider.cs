using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Games
{
    public class GameSystemXmlProvider : BaseXmlProvider, ILocalMetadataProvider<GameSystem>
    {
        private readonly ILogger _logger;

        public GameSystemXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<GameSystem>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<GameSystem>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var item = new GameSystem();

                new GameSystemXmlParser(_logger).Fetch(item, path, cancellationToken);
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

        protected override FileInfo GetXmlFile(string path)
        {
            return new FileInfo(Path.Combine(path, "gamesystem.xml"));
        }
    }
}
