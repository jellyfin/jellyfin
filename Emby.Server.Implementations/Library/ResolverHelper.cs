using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Options;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Class ResolverHelper.
    /// </summary>
    public static class ResolverHelper
    {
        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="metadataOptions">The Metadata options.</param>
        /// <returns>True if initializing was successful.</returns>
        /// <exception cref="ArgumentException">Item must have a path.</exception>
        public static bool SetInitialItemValues(BaseItem item, Folder? parent, ILibraryManager libraryManager, IDirectoryService directoryService, MetadataConfiguration metadataOptions)
        {
            // This version of the below method has no ItemResolveArgs, so we have to require the path already being set
            ArgumentException.ThrowIfNullOrEmpty(item.Path);

            // If the resolver didn't specify this
            if (parent is not null)
            {
                item.SetParent(parent);
            }

            item.Id = libraryManager.GetNewItemId(item.Path, item.GetType());

            item.IsLocked = item.Path.Contains("[dontfetchmeta]", StringComparison.OrdinalIgnoreCase) ||
                item.GetParents().Any(i => i.IsLocked);

            // Make sure DateCreated and DateModified have values
            var fileInfo = directoryService.GetFileSystemEntry(item.Path);
            if (fileInfo is null)
            {
                return false;
            }

            SetDateCreated(item, fileInfo, metadataOptions);

            EnsureName(item, fileInfo);

            return true;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="metadataOptions">The metadata options.</param>
        public static void SetInitialItemValues(BaseItem item, ItemResolveArgs args, IFileSystem fileSystem, ILibraryManager libraryManager, MetadataConfiguration metadataOptions)
        {
            // If the resolver didn't specify this
            if (string.IsNullOrEmpty(item.Path))
            {
                item.Path = args.Path;
            }

            // If the resolver didn't specify this
            if (args.Parent is not null)
            {
                item.SetParent(args.Parent);
            }

            item.Id = libraryManager.GetNewItemId(item.Path, item.GetType());

            // Make sure the item has a name
            EnsureName(item, args.FileInfo);

            item.IsLocked = item.Path.Contains("[dontfetchmeta]", StringComparison.OrdinalIgnoreCase) ||
                item.GetParents().Any(i => i.IsLocked);

            // Make sure DateCreated and DateModified have values
            EnsureDates(fileSystem, item, args, metadataOptions);
        }

        /// <summary>
        /// Ensures the name.
        /// </summary>
        private static void EnsureName(BaseItem item, FileSystemMetadata fileInfo)
        {
            // If the subclass didn't supply a name, add it here
            if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Path))
            {
                item.Name = fileInfo.IsDirectory ? fileInfo.Name : Path.GetFileNameWithoutExtension(fileInfo.Name);
            }
        }

        /// <summary>
        /// Ensures DateCreated and DateModified have values.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="metadataOptions">The Metadata Options.</param>
        private static void EnsureDates(IFileSystem fileSystem, BaseItem item, ItemResolveArgs args, MetadataConfiguration metadataOptions)
        {
            // See if a different path came out of the resolver than what went in
            if (!fileSystem.AreEqual(args.Path, item.Path))
            {
                var childData = args.IsDirectory ? args.GetFileSystemEntryByPath(item.Path) : null;

                if (childData is not null)
                {
                    SetDateCreated(item, childData, metadataOptions);
                }
                else
                {
                    var fileData = fileSystem.GetFileSystemInfo(item.Path);

                    if (fileData.Exists)
                    {
                        SetDateCreated(item, fileData, metadataOptions);
                    }
                }
            }
            else
            {
                SetDateCreated(item, args.FileInfo, metadataOptions);
            }
        }

        private static void SetDateCreated(BaseItem item, FileSystemMetadata? info, MetadataConfiguration metadataOptions)
        {
            var config = metadataOptions;

            if (config.UseFileCreationTimeForDateAdded)
            {
                var fileCreationDate = info?.CreationTimeUtc;
                if (fileCreationDate is not null)
                {
                    var dateCreated = fileCreationDate;
                    if (dateCreated == DateTime.MinValue)
                    {
                        dateCreated = DateTime.UtcNow;
                    }

                    item.DateCreated = dateCreated.Value;
                }
            }
            else
            {
                item.DateCreated = DateTime.UtcNow;
            }

            if (info is not null && !info.IsDirectory)
            {
                item.Size = info.Length;
            }

            var fileModificationDate = info?.LastWriteTimeUtc;
            if (fileModificationDate.HasValue)
            {
                item.DateModified = fileModificationDate.Value;
            }
        }
    }
}
