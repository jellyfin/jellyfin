using System.IO;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Providers
{
    /// <summary>
    /// Class SeriesProviderFromXml
    /// </summary>
    public class SeriesXmlProvider : BaseXmlProvider<Series>, IHasOrder
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

        public SeriesXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        protected override void Fetch(MetadataResult<Series> result, string path, CancellationToken cancellationToken)
        {
            new SeriesXmlParser(_logger, _providerManager, XmlReaderSettingsFactory, FileSystem).Fetch(result, path, cancellationToken);
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
