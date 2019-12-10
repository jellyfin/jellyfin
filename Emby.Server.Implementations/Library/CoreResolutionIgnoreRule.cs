using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Provides the core resolver ignore rules
    /// </summary>
    public class CoreResolutionIgnoreRule : IResolverIgnoreRule
    {
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Any folder named in this list will be ignored
        /// </summary>
        private static readonly string[] _ignoreFolders =
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
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreResolutionIgnoreRule"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        public CoreResolutionIgnoreRule(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent)
        {
            // Don't ignore top level folders
            if (fileInfo.IsDirectory && parent is AggregateFolder)
            {
                return false;
            }

            var filename = fileInfo.Name;

            // Ignore hidden files on UNIX
            if (Environment.OSVersion.Platform != PlatformID.Win32NT
                && filename[0] == '.')
            {
                return true;
            }

            if (fileInfo.IsDirectory)
            {
                // Ignore any folders in our list
                if (_ignoreFolders.Contains(filename, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (parent != null)
                {
                    // Ignore trailer folders but allow it at the collection level
                    if (string.Equals(filename, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase)
                        && !(parent is AggregateFolder)
                        && !(parent is UserRootFolder))
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
                    if (string.Equals(Path.GetFileNameWithoutExtension(filename), BaseItem.ThemeSongFilename)
                        && _libraryManager.IsAudioFile(filename))
                    {
                        return true;
                    }
                }

                // Ignore samples
                Match m = Regex.Match(filename, @"\bsample\b", RegexOptions.IgnoreCase);

                return m.Success;
            }

            return false;
        }
    }
}
