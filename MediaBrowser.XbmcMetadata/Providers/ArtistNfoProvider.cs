using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Parsers;
using System.IO;
using System.Threading;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public class ArtistNfoProvider : BaseNfoProvider<MusicArtist>
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

        public ArtistNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        protected override void Fetch(MetadataResult<MusicArtist> result, string path, CancellationToken cancellationToken)
        {
            new BaseNfoParser<MusicArtist>(_logger, _config, _providerManager, FileSystem, XmlReaderSettingsFactory).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "artist.nfo"));
        }
    }
}
