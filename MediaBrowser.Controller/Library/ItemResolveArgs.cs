#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// These are arguments relating to the file system that are collected once and then referred to
    /// whenever needed.  Primarily for entity resolution.
    /// </summary>
    public class ItemResolveArgs
    {
        /// <summary>
        /// The _app paths.
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;

        private readonly ILibraryManager _libraryManager;
        private LibraryOptions _libraryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemResolveArgs" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        public ItemResolveArgs(IServerApplicationPaths appPaths, ILibraryManager libraryManager)
        {
            _appPaths = appPaths;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets or sets the file system children.
        /// </summary>
        /// <value>The file system children.</value>
        public FileSystemMetadata[] FileSystemChildren { get; set; }

        public LibraryOptions LibraryOptions
        {
            get => _libraryOptions ??= Parent is null ? new LibraryOptions() : _libraryManager.GetLibraryOptions(Parent);
            set => _libraryOptions = value;
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public Folder Parent { get; set; }

        /// <summary>
        /// Gets or sets the file info.
        /// </summary>
        /// <value>The file info.</value>
        public FileSystemMetadata FileInfo { get; set; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path => FileInfo.FullName;

        /// <summary>
        /// Gets a value indicating whether this instance is directory.
        /// </summary>
        /// <value><c>true</c> if this instance is directory; otherwise, <c>false</c>.</value>
        public bool IsDirectory => FileInfo.IsDirectory;

        /// <summary>
        /// Gets a value indicating whether this instance is vf.
        /// </summary>
        /// <value><c>true</c> if this instance is vf; otherwise, <c>false</c>.</value>
        public bool IsVf
        {
            // we should be considered a virtual folder if we are a child of one of the children of the system root folder.
            //  this is a bit of a trick to determine that...  the directory name of a sub-child of the root will start with
            //  the root but not be equal to it
            get
            {
                if (!IsDirectory)
                {
                    return false;
                }

                var parentDir = System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

                return parentDir.Length > _appPaths.RootFolderPath.Length
                       && parentDir.StartsWith(_appPaths.RootFolderPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is physical root.
        /// </summary>
        /// <value><c>true</c> if this instance is physical root; otherwise, <c>false</c>.</value>
        public bool IsPhysicalRoot => IsDirectory && BaseItem.FileSystem.AreEqual(Path, _appPaths.RootFolderPath);

        /// <summary>
        /// Gets or sets the additional locations.
        /// </summary>
        /// <value>The additional locations.</value>
        private List<string> AdditionalLocations { get; set; }

        /// <summary>
        /// Gets the physical locations.
        /// </summary>
        /// <value>The physical locations.</value>
        public string[] PhysicalLocations
        {
            get
            {
                var paths = string.IsNullOrEmpty(Path) ? Array.Empty<string>() : [Path];
                return AdditionalLocations is null ? paths : [..paths, ..AdditionalLocations];
            }
        }

        public CollectionType? CollectionType { get; set; }

        public bool HasParent<T>()
            where T : Folder
        {
            var parent = Parent;

            if (parent is not null)
            {
                var item = parent as T;

                // Just in case the user decided to nest episodes.
                // Not officially supported but in some cases we can handle it.
                if (item is null)
                {
                    var parents = parent.GetParents();
                    foreach (var currentParent in parents)
                    {
                        if (currentParent is T)
                        {
                            return true;
                        }
                    }
                }

                return item is not null;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ItemResolveArgs);
        }

        /// <summary>
        /// Adds the additional location.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        public void AddAdditionalLocation(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            AdditionalLocations ??= new List<string>();
            AdditionalLocations.Add(path);
        }

        // REVIEW: @bond

        /// <summary>
        /// Gets the name of the file system entry by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>FileSystemInfo.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c> or empty.</exception>
        public FileSystemMetadata GetFileSystemEntryByName(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return GetFileSystemEntryByPath(System.IO.Path.Combine(Path, name));
        }

        /// <summary>
        /// Gets the file system entry by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        /// <exception cref="ArgumentNullException">Throws if path is invalid.</exception>
        public FileSystemMetadata GetFileSystemEntryByPath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            foreach (var file in FileSystemChildren)
            {
                if (string.Equals(file.FullName, path, StringComparison.Ordinal))
                {
                    return file;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether [contains file system entry by name] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if [contains file system entry by name] [the specified name]; otherwise, <c>false</c>.</returns>
        public bool ContainsFileSystemEntryByName(string name)
        {
            return GetFileSystemEntryByName(name) is not null;
        }

        public CollectionType? GetCollectionType()
        {
            return CollectionType;
        }

        /// <summary>
        /// Gets the configured content type for the path.
        /// </summary>
        /// <returns>The configured content type.</returns>
        public CollectionType? GetConfiguredContentType()
        {
            return _libraryManager.GetConfiguredContentType(Path);
        }

        /// <summary>
        /// Gets the file system children that do not hit the ignore file check.
        /// </summary>
        /// <returns>The file system children that are not ignored.</returns>
        public IEnumerable<FileSystemMetadata> GetActualFileSystemChildren()
        {
            var numberOfChildren = FileSystemChildren.Length;
            for (var i = 0; i < numberOfChildren; i++)
            {
                var child = FileSystemChildren[i];
                if (_libraryManager.IgnoreFile(child, Parent))
                {
                    continue;
                }

                yield return child;
            }
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return Path.GetHashCode(StringComparison.Ordinal);
        }

        /// <summary>
        /// Equals the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if the arguments are the same, <c>false</c> otherwise.</returns>
        protected bool Equals(ItemResolveArgs args)
        {
            if (args is not null)
            {
                if (args.Path is null && Path is null)
                {
                    return true;
                }

                return args.Path is not null && BaseItem.FileSystem.AreEqual(args.Path, Path);
            }

            return false;
        }
    }
}
