using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using CommonIO;

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
        /// <param name="directoryService">The directory service.</param>
        /// <param name="path">The path.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="args">The args.</param>
        /// <param name="flattenFolderDepth">The flatten folder depth.</param>
        /// <param name="resolveShortcuts">if set to <c>true</c> [resolve shortcuts].</param>
        /// <returns>Dictionary{System.StringFileSystemInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        public static Dictionary<string, FileSystemMetadata> GetFilteredFileSystemEntries(IDirectoryService directoryService,
            string path,
            IFileSystem fileSystem,
            ILogger logger,
            ItemResolveArgs args,
            int flattenFolderDepth = 0,
            bool resolveShortcuts = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (!resolveShortcuts && flattenFolderDepth == 0)
            {
                return directoryService.GetFileSystemDictionary(path);
            }

            var entries = directoryService.GetFileSystemEntries(path);

            var dict = new Dictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                var isDirectory = entry.IsDirectory;

                var fullName = entry.FullName;

                if (resolveShortcuts && fileSystem.IsShortcut(fullName))
                {
                    try
                    {
                        var newPath = fileSystem.ResolveShortcut(fullName);

                        if (string.IsNullOrWhiteSpace(newPath))
                        {
                            //invalid shortcut - could be old or target could just be unavailable
                            logger.Warn("Encountered invalid shortcut: " + fullName);
                            continue;
                        }

                        // Don't check if it exists here because that could return false for network shares.
                        var data = fileSystem.GetDirectoryInfo(newPath);

                        // add to our physical locations
                        args.AddAdditionalLocation(newPath);

                        dict[newPath] = data;
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorException("Error resolving shortcut from {0}", ex, fullName);
                    }
                }
                else if (flattenFolderDepth > 0 && isDirectory)
                {
                    foreach (var child in GetFilteredFileSystemEntries(directoryService, fullName, fileSystem, logger, args, flattenFolderDepth: flattenFolderDepth - 1, resolveShortcuts: resolveShortcuts))
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
