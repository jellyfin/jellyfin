using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class GameXmlProvider : BaseXmlProvider<Game>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public GameXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<Game> result, string path, CancellationToken cancellationToken)
        {
            new GameXmlParser(_logger, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var specificFile = Path.ChangeExtension(info.Path, ".xml");
            var file = FileSystem.GetFileInfo(specificFile);

            return info.IsInMixedFolder || file.Exists ? file : FileSystem.GetFileInfo(Path.Combine(Path.GetDirectoryName(info.Path), "game.xml"));
        }
    }
}
