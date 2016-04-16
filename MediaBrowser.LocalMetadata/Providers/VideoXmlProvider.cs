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

        public VideoXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(MetadataResult<Video> result, string path, CancellationToken cancellationToken)
        {
            new VideoXmlParser(_logger).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
