using MediaBrowser.Controller;
using System.IO;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class DeleteDlnaProfiles : IVersionMigration
    {
        private readonly IServerApplicationPaths _appPaths;

        public DeleteDlnaProfiles(IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public void Run()
        {
            RemoveProfile("Android");
            RemoveProfile("Windows Phone");
            RemoveProfile("Windows 8 RT");
        }

        private void RemoveProfile(string filename)
        {
            try
            {
                File.Delete(Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system", filename + ".xml"));
            }
            catch
            {

            }
            try
            {
                File.Delete(Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user", filename + ".xml"));
            }
            catch
            {

            }
        }
    }
}
