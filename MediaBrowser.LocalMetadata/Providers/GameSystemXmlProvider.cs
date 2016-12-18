using System.IO;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class GameSystemXmlProvider : BaseXmlProvider<GameSystem>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly IXmlReaderSettingsFactory _xmlSettings;

        public GameSystemXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlSettings)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            _xmlSettings = xmlSettings;
        }

        protected override void Fetch(MetadataResult<GameSystem> result, string path, CancellationToken cancellationToken)
        {
            new GameSystemXmlParser(_logger, _providerManager, _xmlSettings, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "gamesystem.xml"));
        }
    }
}
