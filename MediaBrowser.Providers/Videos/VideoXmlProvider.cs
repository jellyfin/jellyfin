using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Movies;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Videos
{
    class VideoXmlProvider : BaseXmlProvider<Video>
    {
        private readonly ILogger _logger;

        public VideoXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Video> result, string path, CancellationToken cancellationToken)
        {
            var chapters = new List<ChapterInfo>();

            new MovieXmlParser(_logger).Fetch(result.Item, chapters, path, cancellationToken);

            result.Chapters = chapters;
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
