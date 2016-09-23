using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Provides the core resolver ignore rules
    /// </summary>
    public class CoreResolutionIgnoreRule : IResolverIgnoreRule
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

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
                ".wd_tv",

                // Synology
                "@eaDir",
                "eaDir",
                "#recycle"

        };
        
        public CoreResolutionIgnoreRule(IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Shoulds the ignore.
        /// </summary>
        /// <param name="fileInfo">The file information.</param>
        /// <param name="parent">The parent.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent)
        {
            var filename = fileInfo.Name;
            var isHidden = (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            var path = fileInfo.FullName;

            // Handle mac .DS_Store
            // https://github.com/MediaBrowser/MediaBrowser/issues/427
            if (filename.IndexOf("._", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            // Ignore hidden files and folders
            if (isHidden)
            {
                if (parent == null)
                {
                    var parentFolderName = Path.GetFileName(Path.GetDirectoryName(path));

                    if (string.Equals(parentFolderName, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    if (string.Equals(parentFolderName, BaseItem.ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // Sometimes these are marked hidden
                if (_fileSystem.IsRootPath(path))
                {
                    return false;
                }

                return true;
            }

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
                    if (string.Equals(_fileSystem.GetFileNameWithoutExtension(filename), BaseItem.ThemeSongFilename) && _libraryManager.IsAudioFile(filename))
                    {
                        return true;
                    }
                }
                
                // Ignore samples
                var sampleFilename = " " + filename.Replace(".", " ", StringComparison.OrdinalIgnoreCase)
                    .Replace("-", " ", StringComparison.OrdinalIgnoreCase)
                    .Replace("_", " ", StringComparison.OrdinalIgnoreCase)
                    .Replace("!", " ", StringComparison.OrdinalIgnoreCase);

                if (sampleFilename.IndexOf(" sample ", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
