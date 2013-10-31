using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.ServerApplication.IO
{
    /// <summary>
    /// Class FileSystemFactory
    /// </summary>
    public static class FileSystemFactory
    {
        /// <summary>
        /// Creates the file system manager.
        /// </summary>
        /// <returns>IFileSystem.</returns>
        public static IFileSystem CreateFileSystemManager(ILogManager logManager)
        {
            return new NativeFileSystem(logManager.GetLogger("FileSystem"));
        }
    }
}
