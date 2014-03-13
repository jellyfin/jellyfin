using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Games
{
    public class GameXmlProvider : BaseXmlProvider<Game>
    {
        private readonly ILogger _logger;

        public GameXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Game> result, string path, CancellationToken cancellationToken)
        {
            new GameXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var fileInfo = FileSystem.GetFileSystemInfo(info.Path);

            var directoryInfo = fileInfo as DirectoryInfo;

            if (directoryInfo == null)
            {
                directoryInfo = new DirectoryInfo(Path.GetDirectoryName(info.Path));
            }

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(info.Path) + ".xml");

            var file = new FileInfo(specificFile);

            return info.IsInMixedFolder || file.Exists ? file : new FileInfo(Path.Combine(directoryPath, "game.xml"));
        }
    }
}
