using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.Threading;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Providers
{
    class VideoXmlProvider : BaseXmlProvider<Video>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public VideoXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<Video> result, string path, CancellationToken cancellationToken)
        {
            new VideoXmlParser(_logger, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
