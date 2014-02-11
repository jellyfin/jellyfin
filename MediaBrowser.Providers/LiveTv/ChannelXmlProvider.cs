using MediaBrowser.Common.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.LiveTv
{
    public class ChannelXmlProvider : BaseXmlProvider<LiveTvChannel>
    {
        private readonly ILogger _logger;

        public ChannelXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<LiveTvChannel> result, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<LiveTvChannel>(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "channel.xml"));
        }
    }
}
