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
                ".mts"
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
        private static readonly Dictionary<string,string> AudioFileExtensions = new[] { 
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

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

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

            return AudioFileExtensions.ContainsKey(extension);
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
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        public static void EnsureDates(BaseItem item, ItemResolveArgs args)
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
                    item.DateCreated = childData.CreationTimeUtc;
                    item.DateModified = childData.LastWriteTimeUtc;
                }
                else
                {
                    var fileData = FileSystem.GetFileSystemInfo(item.Path);

                    if (fileData.Exists)
                    {
                        item.DateCreated = fileData.CreationTimeUtc;
                        item.DateModified = fileData.LastWriteTimeUtc;
                    }
                }
            }
            else
            {
                item.DateCreated = args.FileInfo.CreationTimeUtc;
                item.DateModified = args.FileInfo.LastWriteTimeUtc;
            }
        }
    }
}
