using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class ResolverHelper
    /// </summary>
    public static class ResolverHelper
    {
        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <exception cref="System.ArgumentException">Item must have a path</exception>
        public static void SetInitialItemValues(BaseItem item, Folder parent, IFileSystem fileSystem, ILibraryManager libraryManager, IDirectoryService directoryService)
        {
            // This version of the below method has no ItemResolveArgs, so we have to require the path already being set
            if (string.IsNullOrWhiteSpace(item.Path))
            {
                throw new ArgumentException("Item must have a Path");
            }

            // If the resolver didn't specify this
            if (parent != null)
            {
                item.SetParent(parent);
            }

            item.Id = libraryManager.GetNewItemId(item.Path, item.GetType());

            item.IsLocked = item.Path.IndexOf("[dontfetchmeta]", StringComparison.OrdinalIgnoreCase) != -1 ||
                item.GetParents().Any(i => i.IsLocked);

            // Make sure DateCreated and DateModified have values
            var fileInfo = directoryService.GetFile(item.Path);
            SetDateCreated(item, fileSystem, fileInfo);

            EnsureName(item, fileInfo);
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="libraryManager">The library manager.</param>
        public static void SetInitialItemValues(BaseItem item, ItemResolveArgs args, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            // If the resolver didn't specify this
            if (string.IsNullOrEmpty(item.Path))
            {
                item.Path = args.Path;
            }

            // If the resolver didn't specify this
            if (args.Parent != null)
            {
                item.SetParent(args.Parent);
            }

            item.Id = libraryManager.GetNewItemId(item.Path, item.GetType());

            // Make sure the item has a name
            EnsureName(item, args.FileInfo);

            item.IsLocked = item.Path.IndexOf("[dontfetchmeta]", StringComparison.OrdinalIgnoreCase) != -1 ||
                item.GetParents().Any(i => i.IsLocked);

            // Make sure DateCreated and DateModified have values
            EnsureDates(fileSystem, item, args);
        }

        /// <summary>
        /// Ensures the name.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fileInfo">The file information.</param>
        private static void EnsureName(BaseItem item, FileSystemMetadata fileInfo)
        {
            // If the subclass didn't supply a name, add it here
            if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Path))
            {
                item.Name = GetDisplayName(fileInfo.Name, fileInfo.IsDirectory);
            }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns>System.String.</returns>
        private static string GetDisplayName(string path, bool isDirectory)
        {
           return isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// The MB name regex
        /// </summary>
        private static readonly Regex MbNameRegex = new Regex(@"(\[.*?\])", RegexOptions.Compiled);

        internal static string StripBrackets(string inputString)
        {
            var output = MbNameRegex.Replace(inputString, string.Empty).Trim();
            return Regex.Replace(output, @"\s+", " ");
        }

        /// <summary>
        /// Ensures DateCreated and DateModified have values
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private static void EnsureDates(IFileSystem fileSystem, BaseItem item, ItemResolveArgs args)
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
                    SetDateCreated(item, fileSystem, childData);
                }
                else
                {
                    var fileData = fileSystem.GetFileSystemInfo(item.Path);

                    if (fileData.Exists)
                    {
                        SetDateCreated(item, fileSystem, fileData);
                    }
                }
            }
            else
            {
                SetDateCreated(item, fileSystem, args.FileInfo);
            }
        }

        private static void SetDateCreated(BaseItem item, IFileSystem fileSystem, FileSystemMetadata info)
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
