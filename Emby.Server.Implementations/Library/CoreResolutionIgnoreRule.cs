using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Provides the core resolver ignore rules
    /// </summary>
    public class CoreResolutionIgnoreRule : IResolverIgnoreRule
    {
        private readonly ILibraryManager _libraryManager;

        private bool _ignoreDotPrefix;

        /// <summary>
        /// Any folder named in this list will be ignored - can be added to at runtime for extensibility
        /// </summary>
        public static readonly string[] IgnoreFolders =
        {
                "metadata",
                "ps3_update",
                "ps3_vprm",
                "extrafanart",
                "extrathumbs",
                ".actors",
                ".wd_tv",

                // Synology
                "@eaDir",
                "eaDir",
                "#recycle",

                // Qnap
                "@Recycle",
                ".@__thumb",
                "$RECYCLE.BIN",
                "System Volume Information",
                ".grab",

                // macos
                ".AppleDouble"

        };

        public CoreResolutionIgnoreRule(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;

            _ignoreDotPrefix = Environment.OSVersion.Platform != PlatformID.Win32NT;
        }

        /// <summary>
        /// Shoulds the ignore.
        /// </summary>
        /// <param name="fileInfo">The file information.</param>
        /// <param name="parent">The parent.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent)
        {
            // Don't ignore top level folders
            if (fileInfo.IsDirectory && parent is AggregateFolder)
            {
                return false;
            }

            var filename = fileInfo.Name;
            var path = fileInfo.FullName;

            // Handle mac .DS_Store
            // https://github.com/MediaBrowser/MediaBrowser/issues/427
            if (_ignoreDotPrefix)
            {
                if (filename.IndexOf('.') == 0)
                {
                    return true;
                }
            }

            // Ignore hidden files and folders
            //if (fileInfo.IsHidden)
            //{
            //    if (parent == null)
            //    {
            //        var parentFolderName = Path.GetFileName(_fileSystem.GetDirectoryName(path));

            //        if (string.Equals(parentFolderName, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
            //        {
            //            return false;
            //        }
            //        if (string.Equals(parentFolderName, BaseItem.ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
            //        {
            //            return false;
            //        }
            //    }

            //    // Sometimes these are marked hidden
            //    if (_fileSystem.IsRootPath(path))
            //    {
            //        return false;
            //    }

            //    return true;
            //}

            if (fileInfo.IsDirectory)
            {
                // Ignore any folders in our list
                if (IgnoreFolders.Contains(filename, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (parent != null)
                {
                    // Ignore trailer folders but allow it at the collection level
                    if (string.Equals(filename, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase) &&
                        !(parent is AggregateFolder) && !(parent is UserRootFolder))
                    {
                        return true;
                    }

                    if (string.Equals(filename, BaseItem.ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (string.Equals(filename, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (parent != null)
                {
                    // Don't resolve these into audio files
                    if (string.Equals(Path.GetFileNameWithoutExtension(filename), BaseItem.ThemeSongFilename) && _libraryManager.IsAudioFile(filename))
                    {
                        return true;
                    }
                }

                // Ignore samples
                Match m = Regex.Match(filename,"\bsample\b",RegexOptions.IgnoreCase);

                return m.Success;
            }

            return false;
        }
    }
}
