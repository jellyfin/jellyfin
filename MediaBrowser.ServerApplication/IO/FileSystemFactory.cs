using MediaBrowser.Controller.IO;

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
        public static IFileSystem CreateFileSystemManager()
        {
            return new NativeFileSystem();
        }
    }
}
