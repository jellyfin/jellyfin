using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.IO
{
    /// <summary>
    /// Provides low level File access that is much faster than the File/Directory api's.
    /// </summary>
    public static class FileData
    {
        /// <summary>
        /// Gets the filtered file system entries.
        /// </summary>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="path">The path.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="args">The args.</param>
        /// <param name="flattenFolderDepth">The flatten folder depth.</param>
        /// <param name="resolveShortcuts">if set to <c>true</c> [resolve shortcuts].</param>
        /// <returns>Dictionary{System.StringFileSystemInfo}.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is <c>null</c> or empty.</exception>
        public static FileSystemMetadata[] GetFilteredFileSystemEntries(
            IDirectoryService directoryService,
            string path,
            IFileSystem fileSystem,
            IServerApplicationHost appHost,
            ILogger logger,
            ItemResolveArgs args,
            int flattenFolderDepth = 0,
            bool resolveShortcuts = true)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            ArgumentNullException.ThrowIfNull(args);

            var entries = directoryService.GetFileSystemEntries(path);

            if (!resolveShortcuts && flattenFolderDepth == 0)
            {
                return entries;
            }

            var dict = new Dictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                var isDirectory = entry.IsDirectory;

                var fullName = entry.FullName;

                if (resolveShortcuts && fileSystem.IsShortcut(fullName))
                {
                    try
                    {
                        var newPath = appHost.ExpandVirtualPath(fileSystem.ResolveShortcut(fullName));

                        if (string.IsNullOrEmpty(newPath))
                        {
                            // invalid shortcut - could be old or target could just be unavailable
                            logger.LogWarning("Encountered invalid shortcut: {Path}", fullName);
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
                        logger.LogError(ex, "Error resolving shortcut from {Path}", fullName);
                    }
                }
                else if (flattenFolderDepth > 0 && isDirectory)
                {
                    foreach (var child in GetFilteredFileSystemEntries(directoryService, fullName, fileSystem, appHost, logger, args, flattenFolderDepth: flattenFolderDepth - 1, resolveShortcuts: resolveShortcuts))
                    {
                        dict[child.FullName] = child;
                    }
                }
                else
                {
                    dict[fullName] = entry;
                }
            }

            var returnResult = new FileSystemMetadata[dict.Count];
            var index = 0;
            var values = dict.Values;
            foreach (var value in values)
            {
                returnResult[index] = value;
                index++;
            }

            return returnResult;
        }
    }
}
