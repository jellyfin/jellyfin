using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Games
{
    public class GameSystemXmlProvider : BaseXmlProvider<GameSystem>
    {
        private readonly ILogger _logger;

        public GameSystemXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<GameSystem> result, string path, CancellationToken cancellationToken)
        {
            new GameSystemXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "gamesystem.xml"));
        }
    }
}
