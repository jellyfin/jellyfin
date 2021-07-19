using System;
using System.IO;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Helper class to create async <see cref="FileStream" />s.
    /// </summary>
    public static class AsyncFile
    {
        /// <summary>
        /// Gets a value indicating whether we should use async IO on this platform.
        /// <see href="https://github.com/dotnet/runtime/issues/16354" />.
        /// </summary>
        /// <returns>Returns <c>false</c> on Windows; otherwise <c>true</c>.</returns>
        public static bool UseAsyncIO => !OperatingSystem.IsWindows();

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <returns>A read-only <see cref="FileStream" /> on the specified path.</returns>
        public static FileStream OpenRead(string path)
            => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, IODefaults.FileStreamBufferSize, UseAsyncIO);

        /// <summary>
        /// Opens an existing file for writing.
        /// </summary>
        /// <param name="path">The file to be opened for writing.</param>
        /// <returns>An unshared <see cref="FileStream" /> object on the specified path with Write access.</returns>
        public static FileStream OpenWrite(string path)
            => new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, UseAsyncIO);
    }
}
