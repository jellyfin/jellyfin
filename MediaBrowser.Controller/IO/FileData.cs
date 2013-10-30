using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Controller.IO
{
    /// <summary>
    /// Provides low level File access that is much faster than the File/Directory api's
    /// </summary>
    public static class FileData
    {
        /// <summary>
        /// Gets the filtered file system entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="args">The args.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="flattenFolderDepth">The flatten folder depth.</param>
        /// <param name="resolveShortcuts">if set to <c>true</c> [resolve shortcuts].</param>
        /// <returns>Dictionary{System.StringFileSystemInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        public static Dictionary<string, FileSystemInfo> GetFilteredFileSystemEntries(string path, IFileSystem fileSystem, ILogger logger, ItemResolveArgs args, string searchPattern = "*", int flattenFolderDepth = 0, bool resolveShortcuts = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            var entries = new DirectoryInfo(path).EnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);

            if (!resolveShortcuts && flattenFolderDepth == 0)
            {
                // Seeing dupes on some users file system for some reason
                var dictionary = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);

                foreach (var info in entries)
                {
                    dictionary[info.FullName] = info;
                }

                return dictionary;
            }

            var dict = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                var isDirectory = (entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                var fullName = entry.FullName;

                if (resolveShortcuts && fileSystem.IsShortcut(fullName))
                {
                    var newPath = fileSystem.ResolveShortcut(fullName);

                    if (string.IsNullOrWhiteSpace(newPath))
                    {
                        //invalid shortcut - could be old or target could just be unavailable
                        logger.Warn("Encountered invalid shortcut: " + fullName);
                        continue;
                    }

                    // Don't check if it exists here because that could return false for network shares.
                    var data = new DirectoryInfo(newPath);

                    // add to our physical locations
                    args.AddAdditionalLocation(newPath);

                    dict[newPath] = data;
                }
                else if (flattenFolderDepth > 0 && isDirectory)
                {
                    foreach (var child in GetFilteredFileSystemEntries(fullName, fileSystem, logger, args, flattenFolderDepth: flattenFolderDepth - 1, resolveShortcuts: resolveShortcuts))
                    {
                        dict[child.Key] = child.Value;
                    }
                }
                else
                {
                    dict[fullName] = entry;
                }
            }

            return dict;
        }

    }

}
