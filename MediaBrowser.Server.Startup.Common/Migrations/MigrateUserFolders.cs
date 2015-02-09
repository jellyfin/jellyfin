using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class MigrateUserFolders : IVersionMigration
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public MigrateUserFolders(IServerApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public void Run()
        {
            try
            {
                var rootPath = _appPaths.RootFolderPath;

                var folders = new DirectoryInfo(rootPath).EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(i => !string.Equals(i.Name, "default", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var folder in folders)
                {
                    _fileSystem.DeleteDirectory(folder.FullName, true);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
