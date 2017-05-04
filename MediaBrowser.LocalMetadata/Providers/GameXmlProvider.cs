using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class GameXmlProvider : BaseXmlProvider<Game>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly IXmlReaderSettingsFactory _xmlSettings;

        public GameXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlSettings)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            _xmlSettings = xmlSettings;
        }

        protected override void Fetch(MetadataResult<Game> result, string path, CancellationToken cancellationToken)
        {
            new GameXmlParser(_logger, _providerManager, _xmlSettings, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var specificFile = Path.ChangeExtension(info.Path, ".xml");
            var file = FileSystem.GetFileInfo(specificFile);

            return info.IsInMixedFolder || file.Exists ? file : FileSystem.GetFileInfo(Path.Combine(FileSystem.GetDirectoryName(info.Path), "game.xml"));
        }
    }
}
