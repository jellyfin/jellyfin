using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.TV
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

            new EpisodeXmlParser(_logger).Fetch(result.Item, images, path, cancellationToken);

            result.Images = images;
        }

        protected override FileInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var metadataPath = Path.GetDirectoryName(info.Path);
            metadataPath = Path.Combine(metadataPath, "metadata");

            var metadataFile = Path.Combine(metadataPath, Path.ChangeExtension(Path.GetFileName(info.Path), ".xml"));

            return directoryService.GetFile(metadataFile);
        }
    }
}
