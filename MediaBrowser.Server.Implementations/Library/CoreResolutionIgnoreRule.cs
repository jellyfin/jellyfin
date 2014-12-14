using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                ".wd_tv"

        };
        
        public CoreResolutionIgnoreRule(IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Shoulds the ignore.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ShouldIgnore(ItemResolveArgs args)
        {
            var filename = args.FileInfo.Name;

            // Handle mac .DS_Store
            // https://github.com/MediaBrowser/MediaBrowser/issues/427
            if (filename.IndexOf("._", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            // Ignore hidden files and folders
            if (args.IsHidden)
            {
                var parentFolderName = Path.GetFileName(Path.GetDirectoryName(args.Path));

                if (string.Equals(parentFolderName, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (string.Equals(parentFolderName, BaseItem.ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Sometimes these are marked hidden
                if (_fileSystem.IsRootPath(args.Path))
                {
                    return false;
                }

                return true;
            }

            if (args.IsDirectory)
            {
                // Ignore any folders in our list
                if (IgnoreFolders.Contains(filename, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Ignore trailer folders but allow it at the collection level
                if (string.Equals(filename, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase) &&
                    !(args.Parent is AggregateFolder) && !(args.Parent is UserRootFolder))
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
            else
            {
                if (args.Parent != null)
                {
                    // Don't resolve these into audio files
                    if (string.Equals(_fileSystem.GetFileNameWithoutExtension(filename), BaseItem.ThemeSongFilename) && _libraryManager.IsAudioFile(filename))
                    {
                        return true;
                    }
                }
                
                // Ignore samples
                if (filename.IndexOf(".sample.", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
