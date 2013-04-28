using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Provides the core resolver ignore rules
    /// </summary>
    public class CoreResolutionIgnoreRule : IResolverIgnoreRule
    {
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

                if (string.Equals(parentFolderName, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase) || string.Equals(parentFolderName, BaseItem.VideoBackdropsFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
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

                if (string.Equals(filename, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(filename, BaseItem.VideoBackdropsFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(filename, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
