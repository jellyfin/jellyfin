using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using System.IO;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class DeleteDlnaProfiles : IVersionMigration
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public DeleteDlnaProfiles(IServerApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
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
                _fileSystem.DeleteFile(Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system", filename + ".xml"));
            }
            catch
            {

            }
            try
            {
                _fileSystem.DeleteFile(Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user", filename + ".xml"));
            }
            catch
            {

            }
        }
    }
}
