using MediaBrowser.Controller;
using System.IO;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class DeprecatePlugins : IVersionMigration
    {
        private readonly IServerApplicationPaths _appPaths;

        public DeprecatePlugins(IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public void Run()
        {
            RemovePlugin("MediaBrowser.Plugins.LocalTrailers.dll");
        }

        private void RemovePlugin(string filename)
        {
            try
            {
                File.Delete(Path.Combine(_appPaths.PluginsPath, filename));
            }
            catch
            {
                
            }
        }
    }
}
