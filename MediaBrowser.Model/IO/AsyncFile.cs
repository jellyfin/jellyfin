using System.IO;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Helper class to create async <see cref="FileStream" />s.
    /// </summary>
    public static class AsyncFile
    {
        /// <summary>
        /// Gets the default <see cref="FileStreamOptions"/> for reading files async.
        /// </summary>
        public static FileStreamOptions ReadOptions => new FileStreamOptions()
        {
            Options = FileOptions.Asynchronous
        };

        /// <summary>
        /// Gets the default <see cref="FileStreamOptions"/> for writing files async.
        /// </summary>
        public static FileStreamOptions WriteOptions => new FileStreamOptions()
        {
            Mode = FileMode.OpenOrCreate,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        };

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <returns>A read-only <see cref="FileStream" /> on the specified path.</returns>
        public static FileStream OpenRead(string path)
            => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);

        /// <summary>
        /// Opens an existing file for writing.
        /// </summary>
        /// <param name="path">The file to be opened for writing.</param>
        /// <returns>An unshared <see cref="FileStream" /> object on the specified path with Write access.</returns>
        public static FileStream OpenWrite(string path)
            => new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
    }
}
