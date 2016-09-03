using System.IO;
using System.Threading;
using CommonIO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Providers
{
    /// <summary>
    /// Class SeriesProviderFromXml
    /// </summary>
    public class SeriesXmlProvider : BaseXmlProvider<Series>, IHasOrder
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public SeriesXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<Series> result, string path, CancellationToken cancellationToken)
        {
            new SeriesXmlParser(_logger, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "series.xml"));
        }

        public override int Order
        {
            get
            {
                // After Xbmc
                return 1;
            }
        }
    }
}
