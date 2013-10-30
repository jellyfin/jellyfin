using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Class EntityResolutionHelper
    /// </summary>
    public static class EntityResolutionHelper
    {
        /// <summary>
        /// Any extension in this list is considered a video file - can be added to at runtime for extensibility
        /// </summary>
        public static List<string> VideoFileExtensions = new List<string>
            {
                ".mkv",
                ".m2t",
                ".m2ts",
                ".img",
                ".iso",
                ".mk3d",
                ".ts",
                ".rmvb",
                ".mov",
                ".avi",
                ".mpg",
                ".mpeg",
                ".wmv",
                ".mp4",
                ".divx",
                ".dvr-ms",
                ".wtv",
                ".ogm",
                ".ogv",
                ".asf",
                ".m4v",
                ".flv",
                ".f4v",
                ".3gp",
                ".webm",
                ".mts",
                ".rec"
        };

        private static readonly Dictionary<string, string> VideoFileExtensionsDictionary = VideoFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        private static readonly Regex MultiFileRegex = new Regex(
            @"(.*?)([ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck]|d)[ _.-]*[0-9]+)(.*?)(\.[^.]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MultiFolderRegex = new Regex(
            @"(.*?)([ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck]|d)[ _.-]*[0-9]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Determines whether [is multi part file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is multi part file] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsMultiPartFile(string path)
        {
            return MultiFileRegex.Match(path).Success || MultiFolderRegex.Match(path).Success;
        }

        /// <summary>
        /// The audio file extensions
        /// </summary>
        public static readonly string[] AudioFileExtensions = new[]
            {
                ".mp3",
                ".flac",
                ".wma",
                ".aac",
                ".acc",
                ".m4a",
                ".m4b",
                ".wav",
                ".ape",
                ".ogg",
                ".oga"

            };

        private static readonly Dictionary<string, string> AudioFileExtensionsDictionary = AudioFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether [is audio file] [the specified args].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is audio file] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return AudioFileExtensionsDictionary.ContainsKey(extension);
        }

        /// <summary>
        /// Determines whether [is video file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is video file] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsVideoFile(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return VideoFileExtensionsDictionary.ContainsKey(extension);
        }

        /// <summary>
        /// Ensures DateCreated and DateModified have values
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="includeCreationTime">if set to <c>true</c> [include creation time].</param>
        public static void EnsureDates(IFileSystem fileSystem, BaseItem item, ItemResolveArgs args, bool includeCreationTime)
        {
            if (!Path.IsPathRooted(item.Path))
            {
                return;
            }

            // See if a different path came out of the resolver than what went in
            if (!string.Equals(args.Path, item.Path, StringComparison.OrdinalIgnoreCase))
            {
                var childData = args.IsDirectory ? args.GetFileSystemEntryByPath(item.Path) : null;

                if (childData != null)
                {
                    if (includeCreationTime)
                    {
                        item.DateCreated = fileSystem.GetCreationTimeUtc(childData);
                    }

                    item.DateModified = childData.LastWriteTimeUtc;
                }
                else
                {
                    var fileData = fileSystem.GetFileSystemInfo(item.Path);

                    if (fileData.Exists)
                    {
                        if (includeCreationTime)
                        {
                            item.DateCreated = fileSystem.GetCreationTimeUtc(fileData);
                        }
                        item.DateModified = fileData.LastWriteTimeUtc;
                    }
                }
            }
            else
            {
                if (includeCreationTime)
                {
                    item.DateCreated = fileSystem.GetCreationTimeUtc(args.FileInfo);
                }
                item.DateModified = args.FileInfo.LastWriteTimeUtc;
            }
        }
    }
}
