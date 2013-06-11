using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Logging;
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
        private readonly ILogger _logger;

        /// <summary>
        /// Any folder named in this list will be ignored - can be added to at runtime for extensibility
        /// </summary>
        private static readonly List<string> IgnoreFolders = new List<string>
        {
            "metadata",
            "certificate",
            "backup",
            "ps3_update",
            "ps3_vprm",
            "adv_obj",
            "extrafanart"
        };

        public CoreResolutionIgnoreRule(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Shoulds the ignore.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ShouldIgnore(ItemResolveArgs args)
        {
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

                // Drives will sometimes be hidden
                if (args.Path.EndsWith(Path.VolumeSeparatorChar + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Shares will sometimes be hidden
                if (args.Path.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                {
                    // Look for a share, e.g. \\server\movies
                    // Is there a better way to detect if a path is a share without using native code?
                    if (args.Path.Substring(2).Split(Path.DirectorySeparatorChar).Length == 2)
                    {
                        return false;
                    }
                }

                return true;
            }

            if (args.IsDirectory)
            {
                var filename = args.FileInfo.Name;

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
                    var filename = args.FileInfo.Name;

                    if (string.Equals(Path.GetFileNameWithoutExtension(filename), BaseItem.ThemeSongFilename) && EntityResolutionHelper.IsAudioFile(filename))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
