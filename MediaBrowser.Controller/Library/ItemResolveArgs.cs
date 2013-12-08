using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// These are arguments relating to the file system that are collected once and then referred to
    /// whenever needed.  Primarily for entity resolution.
    /// </summary>
    public class ItemResolveArgs : EventArgs
    {
        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;
        private readonly ILibraryManager _libraryManager;

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
        /// Gets the file system children.
        /// </summary>
        /// <value>The file system children.</value>
        public IEnumerable<FileSystemInfo> FileSystemChildren
        {
            get { return FileSystemDictionary.Values; }
        }

        /// <summary>
        /// Gets or sets the file system dictionary.
        /// </summary>
        /// <value>The file system dictionary.</value>
        public Dictionary<string, FileSystemInfo> FileSystemDictionary { get; set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public Folder Parent { get; set; }

        /// <summary>
        /// Gets or sets the file info.
        /// </summary>
        /// <value>The file info.</value>
        public FileSystemInfo FileInfo { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is directory.
        /// </summary>
        /// <value><c>true</c> if this instance is directory; otherwise, <c>false</c>.</value>
        public bool IsDirectory
        {
            get
            {
                return (FileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        public bool IsHidden
        {
            get
            {
                return (FileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is system file.
        /// </summary>
        /// <value><c>true</c> if this instance is system file; otherwise, <c>false</c>.</value>
        public bool IsSystemFile
        {
            get
            {
                return (FileInfo.Attributes & FileAttributes.System) == FileAttributes.System;
            }
        }

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

                return (parentDir.Length > _appPaths.RootFolderPath.Length
                    && parentDir.StartsWith(_appPaths.RootFolderPath, StringComparison.OrdinalIgnoreCase));

            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is physical root.
        /// </summary>
        /// <value><c>true</c> if this instance is physical root; otherwise, <c>false</c>.</value>
        public bool IsPhysicalRoot
        {
            get
            {
                return IsDirectory && string.Equals(Path, _appPaths.RootFolderPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public bool IsRoot
        {
            get
            {
                return Parent == null;
            }
        }

        /// <summary>
        /// Gets or sets the additional locations.
        /// </summary>
        /// <value>The additional locations.</value>
        private List<string> AdditionalLocations { get; set; }

        /// <summary>
        /// Adds the additional location.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddAdditionalLocation(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            
            if (AdditionalLocations == null)
            {
                AdditionalLocations = new List<string>();
            }

            AdditionalLocations.Add(path);
        }

        /// <summary>
        /// Gets the physical locations.
        /// </summary>
        /// <value>The physical locations.</value>
        public IEnumerable<string> PhysicalLocations
        {
            get
            {
                var paths = string.IsNullOrWhiteSpace(Path) ? new string[] {} : new[] {Path};
                return AdditionalLocations == null ? paths : paths.Concat(AdditionalLocations);
            }
        }

        /// <summary>
        /// Store these to reduce disk access in Resolvers
        /// </summary>
        /// <value>The metadata file dictionary.</value>
        private Dictionary<string, FileSystemInfo> MetadataFileDictionary { get; set; }

        /// <summary>
        /// Gets the metadata files.
        /// </summary>
        /// <value>The metadata files.</value>
        public IEnumerable<FileSystemInfo> MetadataFiles
        {
            get
            {
                if (MetadataFileDictionary != null)
                {
                    return MetadataFileDictionary.Values;
                }

                return new FileSystemInfo[] { };
            }
        }

        /// <summary>
        /// Adds the metadata file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public void AddMetadataFile(string path)
        {
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                throw new FileNotFoundException(path);
            }

            AddMetadataFile(file);
        }

        /// <summary>
        /// Adds the metadata file.
        /// </summary>
        /// <param name="fileInfo">The file info.</param>
        public void AddMetadataFile(FileSystemInfo fileInfo)
        {
            AddMetadataFiles(new[] { fileInfo });
        }

        /// <summary>
        /// Adds the metadata files.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddMetadataFiles(IEnumerable<FileSystemInfo> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException();
            }

            if (MetadataFileDictionary == null)
            {
                MetadataFileDictionary = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (var file in files)
            {
                MetadataFileDictionary[file.Name] = file;
            }
        }

        /// <summary>
        /// Gets the name of the file system entry by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>FileSystemInfo.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public FileSystemInfo GetFileSystemEntryByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException();
            }
            
            return GetFileSystemEntryByPath(System.IO.Path.Combine(Path, name));
        }

        /// <summary>
        /// Gets the file system entry by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public FileSystemInfo GetFileSystemEntryByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            
            if (FileSystemDictionary != null)
            {
                FileSystemInfo entry;

                if (FileSystemDictionary.TryGetValue(path, out entry))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the meta file by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public FileSystemInfo GetMetaFileByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            
            if (MetadataFileDictionary != null)
            {
                FileSystemInfo entry;

                if (MetadataFileDictionary.TryGetValue(System.IO.Path.GetFileName(path), out entry))
                {
                    return entry;
                }
            }

            return GetFileSystemEntryByPath(path);
        }

        /// <summary>
        /// Gets the name of the meta file by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>FileSystemInfo.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public FileSystemInfo GetMetaFileByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException();
            }
            
            if (MetadataFileDictionary != null)
            {
                FileSystemInfo entry;

                if (MetadataFileDictionary.TryGetValue(name, out entry))
                {
                    return entry;
                }
            }

            return GetFileSystemEntryByName(name);
        }

        /// <summary>
        /// Determines whether [contains meta file by name] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if [contains meta file by name] [the specified name]; otherwise, <c>false</c>.</returns>
        public bool ContainsMetaFileByName(string name)
        {
            return GetMetaFileByName(name) != null;
        }

        /// <summary>
        /// Determines whether [contains file system entry by name] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if [contains file system entry by name] [the specified name]; otherwise, <c>false</c>.</returns>
        public bool ContainsFileSystemEntryByName(string name)
        {
            return GetFileSystemEntryByName(name) != null;
        }

        private bool _collectionTypeDiscovered;
        private string _collectionType;

        public string GetCollectionType()
        {
            if (!_collectionTypeDiscovered)
            {
                _collectionType = Parent == null ? null : _libraryManager.FindCollectionType(Parent);
                _collectionTypeDiscovered = true;
            }

            return _collectionType;
        }

        #region Equality Overrides

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return (Equals(obj as ItemResolveArgs));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        /// <summary>
        /// Equalses the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected bool Equals(ItemResolveArgs args)
        {
            if (args != null)
            {
                if (args.Path == null && Path == null) return true;
                return args.Path != null && args.Path.Equals(Path, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        #endregion
    }

}
