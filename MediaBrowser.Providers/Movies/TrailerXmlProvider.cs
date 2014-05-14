using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Movies
{
    public class TrailerXmlProvider : BaseXmlProvider<Trailer>
    {
        private readonly ILogger _logger;

        public TrailerXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Trailer> result, string path, CancellationToken cancellationToken)
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
