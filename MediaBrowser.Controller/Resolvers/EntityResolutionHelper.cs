using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Library;

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

        /// <summary>
        /// The audio file extensions
        /// </summary>
        private static readonly string[] AudioFileExtensions = new[] { 
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

        /// <summary>
        /// Determines whether [is audio file] [the specified args].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is audio file] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(string path)
        {
            return AudioFileExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is video file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is video file] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsVideoFile(string path)
        {
            var extension = Path.GetExtension(path) ?? String.Empty;
            return VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
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
            if (!args.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase))
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
