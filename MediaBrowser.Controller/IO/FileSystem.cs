using MediaBrowser.Model.Logging;
using System;
using System.IO;

namespace MediaBrowser.Controller.IO
{
    /// <summary>
    /// Class FileSystem
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>DateTime.</returns>
        public static DateTime GetLastWriteTimeUtc(FileSystemInfo info, ILogger logger)
        {
            // This could throw an error on some file systems that have dates out of range

            try
            {
                return info.LastWriteTimeUtc;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error determining LastAccessTimeUtc for {0}", ex, info.FullName);
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Copies all.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="System.ArgumentNullException">source</exception>
        /// <exception cref="System.ArgumentException">The source and target directories are the same</exception>
        public static void CopyAll(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The source and target directories are the same");
            }

            // Check if the target directory exists, if not, create it. 
            Directory.CreateDirectory(target);

            foreach (var file in Directory.EnumerateFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            // Copy each subdirectory using recursion. 
            foreach (var dir in Directory.EnumerateDirectories(source))
            {
                CopyAll(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }
    }
}
