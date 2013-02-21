using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// This is an EventArgs object used when resolving a Path into a BaseItem
    /// </summary>
    public class ItemResolveEventArgs : PreBeginResolveEventArgs
    {
        public WIN32_FIND_DATA[] FileSystemChildren { get; set; }

        protected List<string> _additionalLocations = new List<string>();
        public List<string> AdditionalLocations
        {
            get
            {
                return _additionalLocations;
            }
            set
            {
                _additionalLocations = value;
            }
        }

        public IEnumerable<string> PhysicalLocations
        {
            get
            {
                return (new List<string>() {this.Path}).Concat(AdditionalLocations);
            }
        }

        public bool IsBDFolder { get; set; }
        public bool IsDVDFolder { get; set; }
        public bool IsHDDVDFolder { get; set; }

        /// <summary>
        /// Store these to reduce disk access in Resolvers
        /// </summary>
        public string[] MetadataFiles { get; set; }

        public WIN32_FIND_DATA? GetFileSystemEntry(string path)
        {
            WIN32_FIND_DATA entry = FileSystemChildren.FirstOrDefault(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            return entry.cFileName != null ? (WIN32_FIND_DATA?)entry : null;
        }

        public bool ContainsFile(string name)
        {
            return FileSystemChildren.FirstOrDefault(f => f.cFileName.Equals(name, StringComparison.OrdinalIgnoreCase)).cFileName != null;
        }

        public bool ContainsFolder(string name)
        {
            return ContainsFile(name);
        }
    }

    /// <summary>
    /// This is an EventArgs object used before we begin resolving a Path into a BaseItem
    /// File system children have not been collected yet, but consuming events will
    /// have a chance to cancel resolution based on the Path, Parent and FileAttributes
    /// </summary>
    public class PreBeginResolveEventArgs : EventArgs
    {
        public Folder Parent { get; set; }

        public bool Cancel { get; set; }

        public WIN32_FIND_DATA FileInfo { get; set; }

        public string Path { get; set; }

        public bool IsDirectory
        {
            get
            {
                return FileInfo.dwFileAttributes.HasFlag(FileAttributes.Directory);
            }
        }

        public bool IsHidden
        {
            get
            {
                return FileInfo.IsHidden;
            }
        }

        public bool IsSystemFile
        {
            get
            {
                return FileInfo.IsSystemFile;
            }
        }

    }
}
