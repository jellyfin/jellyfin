using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Providers
{
    /// <summary>
    /// Class BoxSetXmlProvider.
    /// </summary>
    public class BoxSetXmlProvider : BaseXmlProvider<BoxSet>
    {
        private readonly ILogger _logger;

        public BoxSetXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(MetadataResult<BoxSet> result, string path, CancellationToken cancellationToken)
        {
            new BoxSetXmlParser(_logger).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "collection.xml"));
        }
    }
}
