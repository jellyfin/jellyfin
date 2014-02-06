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

        protected override void Fetch(GameSystem item, string path, CancellationToken cancellationToken)
        {
            new GameSystemXmlParser(_logger).Fetch(item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info)
        {
            return new FileInfo(Path.Combine(info.Path, "gamesystem.xml"));
        }
    }
}
