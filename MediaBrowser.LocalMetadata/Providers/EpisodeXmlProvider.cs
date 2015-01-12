using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class EpisodeXmlProvider : BaseXmlProvider<Episode>
    {
        private readonly ILogger _logger;

        public EpisodeXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Episode> result, string path, CancellationToken cancellationToken)
        {
            var images = new List<LocalImageInfo>();
            var chapters = new List<ChapterInfo>();

            new EpisodeXmlParser(_logger).Fetch(result.Item, images, chapters, path, cancellationToken);

            result.Images = images;
            result.Chapters = chapters;
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var metadataPath = Path.GetDirectoryName(info.Path);
            metadataPath = Path.Combine(metadataPath, "metadata");

            var metadataFile = Path.Combine(metadataPath, Path.ChangeExtension(Path.GetFileName(info.Path), ".xml"));

            return directoryService.GetFile(metadataFile);
        }
    }
}
