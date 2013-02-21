using MediaBrowser.Common.IO;
using MediaBrowser.Common.Win32;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Class EntityResolutionHelper
    /// </summary>
    public static class EntityResolutionHelper
    {
        /// <summary>
        /// Any extension in this list is considered a metadata file - can be added to at runtime for extensibility
        /// </summary>
        public static List<string> MetaExtensions = new List<string>
        {
            ".xml",
            ".jpg",
            ".png",
            ".json",
            ".data"
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
                ".webm"
        };

        /// <summary>
        /// Determines whether [is video file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is video file] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsVideoFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The audio file extensions
        /// </summary>
        public static readonly string[] AudioFileExtensions = new[] { 
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
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is audio file] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(ItemResolveArgs args)
        {
            return AudioFileExtensions.Contains(Path.GetExtension(args.Path), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is audio file] [the specified file].
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns><c>true</c> if [is audio file] [the specified file]; otherwise, <c>false</c>.</returns>
        public static bool IsAudioFile(WIN32_FIND_DATA file)
        {
            return AudioFileExtensions.Contains(Path.GetExtension(file.Path), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determine if the supplied file data points to a music album
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns><c>true</c> if [is music album] [the specified data]; otherwise, <c>false</c>.</returns>
        public static bool IsMusicAlbum(WIN32_FIND_DATA data)
        {
            return ContainsMusic(FileSystem.GetFiles(data.Path));
        }

        /// <summary>
        /// Determine if the supplied reslove args should be considered a music album
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is music album] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsMusicAlbum(ItemResolveArgs args)
        {
            // Args points to an album if parent is an Artist folder or it directly contains music
            if (args.IsDirectory)
            {
                //if (args.Parent is MusicArtist) return true;  //saves us from testing children twice
                if (ContainsMusic(args.FileSystemChildren)) return true;
            }


            return false;
        }

        /// <summary>
        /// Determine if the supplied list contains what we should consider music
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns><c>true</c> if the specified list contains music; otherwise, <c>false</c>.</returns>
        public static bool ContainsMusic(IEnumerable<WIN32_FIND_DATA> list)
        {
            // If list contains at least 2 audio files or at least one and no video files consider it to contain music
            var foundAudio = 0;
            var foundVideo = 0;
            foreach (var file in list)
            {
                if (IsAudioFile(file)) foundAudio++;
                if (foundAudio >= 2)
                {
                    return true;
                }
                if (IsVideoFile(file.Path)) foundVideo++;
            }

            //  or a single audio file and no video files
            if (foundAudio > 0 && foundVideo == 0) return true;
            return false;
        }

        /// <summary>
        /// Determines whether a path should be ignored based on its contents - called after the contents have been read
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public static bool ShouldResolvePathContents(ItemResolveArgs args)
        {
            // Ignore any folders containing a file called .ignore
            return !args.ContainsFileSystemEntryByName(".ignore");
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

                if (childData.HasValue)
                {
                    item.DateCreated = childData.Value.CreationTimeUtc;
                    item.DateModified = childData.Value.LastWriteTimeUtc;
                }
                else
                {
                    var fileData = FileSystem.GetFileData(item.Path);

                    if (fileData.HasValue)
                    {
                        item.DateCreated = fileData.Value.CreationTimeUtc;
                        item.DateModified = fileData.Value.LastWriteTimeUtc;
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
