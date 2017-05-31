using System.Threading;

using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Providers
{
    class MusicVideoXmlProvider : BaseXmlProvider<MusicVideo>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

        public MusicVideoXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        protected override void Fetch(MetadataResult<MusicVideo> result, string path, CancellationToken cancellationToken)
        {
            new MusicVideoXmlParser(_logger, _providerManager, XmlReaderSettingsFactory, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
