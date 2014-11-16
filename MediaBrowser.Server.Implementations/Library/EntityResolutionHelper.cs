using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Server.Implementations.Library
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
                        SetDateCreated(item, fileSystem, childData);
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
                            SetDateCreated(item, fileSystem, fileData);
                        }
                        item.DateModified = fileSystem.GetLastWriteTimeUtc(fileData);
                    }
                }
            }
            else
            {
                if (includeCreationTime)
                {
                    SetDateCreated(item, fileSystem, args.FileInfo);
                }
                item.DateModified = fileSystem.GetLastWriteTimeUtc(args.FileInfo);
            }
        }

        private static void SetDateCreated(BaseItem item, IFileSystem fileSystem, FileSystemInfo info)
        {
            var config = BaseItem.ConfigurationManager.GetMetadataConfiguration();

            if (config.UseFileCreationTimeForDateAdded)
            {
                item.DateCreated = fileSystem.GetCreationTimeUtc(info);
            }
            else
            {
                item.DateCreated = DateTime.UtcNow;
            }
        }
    }
}
