using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Server.Implementations.Playlists;

namespace Emby.Server.Implementations.Playlists
{
    public class PlaylistsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public PlaylistsDynamicFolder(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "playlists");

            _fileSystem.CreateDirectory(path);

            return new PlaylistsFolder
            {
                Path = path
            };
        }
    }
}
