using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using System.IO;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class DeprecatePlugins : IVersionMigration
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public DeprecatePlugins(IServerApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public void Run()
        {
            RemovePlugin("MediaBrowser.Plugins.LocalTrailers.dll");
        }

        private void RemovePlugin(string filename)
        {
            try
            {
                _fileSystem.DeleteFile(Path.Combine(_appPaths.PluginsPath, filename));
            }
            catch
            {
                
            }
        }
    }
}
