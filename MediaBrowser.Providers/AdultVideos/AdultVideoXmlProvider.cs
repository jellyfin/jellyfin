using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Movies;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.AdultVideos
{
    class AdultVideoXmlProvider : BaseXmlProvider<AdultVideo>
    {
        private readonly ILogger _logger;

        public AdultVideoXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<AdultVideo> result, string path, CancellationToken cancellationToken)
        {
            new MovieXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
