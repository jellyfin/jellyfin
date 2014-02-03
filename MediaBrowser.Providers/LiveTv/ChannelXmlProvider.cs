using MediaBrowser.Common.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.LiveTv
{
    public class ChannelXmlProvider : BaseXmlProvider, ILocalMetadataProvider<LiveTvChannel>
    {
        private readonly ILogger _logger;

        public ChannelXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<LiveTvChannel>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<LiveTvChannel>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var item = new LiveTvChannel();

                new BaseItemXmlParser<LiveTvChannel>(_logger).Fetch(item, path, cancellationToken);
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
            return new FileInfo(Path.Combine(path, "channel.xml"));
        }
    }
}
