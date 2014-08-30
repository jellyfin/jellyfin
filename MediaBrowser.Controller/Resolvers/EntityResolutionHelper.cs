using System.Globalization;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
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
        /// Any folder named in this list will be ignored - can be added to at runtime for extensibility
        /// </summary>
        public static readonly List<string> IgnoreFolders = new List<string>
        {
                "metadata",
                "ps3_update",
                "ps3_vprm",
                "extrafanart",
                "extrathumbs",
                ".actors",
                ".wd_tv"

        };

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
                ".m2v",
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
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            path = Path.GetFileName(path);

            return MultiFileRegex.Match(path).Success;
        }

        public static bool IsMultiPartFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            path = Path.GetFileName(path);

            return MultiFolderRegex.Match(path).Success;
        }

        /// <summary>
        /// The audio file extensions
        /// </summary>
        public static readonly string[] AudioFileExtensions =
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

            //".asf",
            //".mp4"
        };

        private static readonly Dictionary<string, string> AudioFileExtensionsDictionary = AudioFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether [is audio file] [the specified args].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is audio file] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

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
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return VideoFileExtensionsDictionary.ContainsKey(extension);
        }

        /// <summary>
        /// Determines whether [is place holder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is place holder] [the specified path]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        public static bool IsVideoPlaceHolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var extension = Path.GetExtension(path);

            return string.Equals(extension, ".disc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is multi disc album folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is multi disc album folder] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsMultiDiscAlbumFolder(string path)
        {
            var filename = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(filename))
            {
                return false;
            }

            // Normalize
            // Remove whitespace
            filename = filename.Replace("-", string.Empty);
            filename = Regex.Replace(filename, @"\s+", "");

            var prefixes = new[] { "disc", "cd", "disk" };

            foreach (var prefix in prefixes)
            {
                if (filename.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var tmp = filename.Substring(prefix.Length);

                    int val;
                    if (int.TryParse(tmp, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    {
                        return true;
                    }
                }
            }

            return false;
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
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
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

                    item.DateModified = fileSystem.GetLastWriteTimeUtc(childData);
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
                        item.DateModified = fileSystem.GetLastWriteTimeUtc(fileData);
                    }
                }
            }
            else
            {
                if (includeCreationTime)
                {
                    item.DateCreated = fileSystem.GetCreationTimeUtc(args.FileInfo);
                }
                item.DateModified = fileSystem.GetLastWriteTimeUtc(args.FileInfo);
            }
        }
    }
}
