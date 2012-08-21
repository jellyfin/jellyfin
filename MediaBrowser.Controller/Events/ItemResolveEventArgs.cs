using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.IO;

namespace MediaBrowser.Controller.Events
{
    /// <summary>
    /// This is an EventArgs object used when resolving a Path into a BaseItem
    /// </summary>
    public class ItemResolveEventArgs : PreBeginResolveEventArgs
    {
        public KeyValuePair<string, WIN32_FIND_DATA>[] FileSystemChildren { get; set; }

        public KeyValuePair<string, WIN32_FIND_DATA>? GetFileSystemEntry(string path, bool? isFolder)
        {
            foreach (KeyValuePair<string, WIN32_FIND_DATA> entry in FileSystemChildren)
            {
                if (isFolder.HasValue)
                {
                    if (isFolder.Value && entry.Value.IsDirectory)
                    {
                        continue;
                    }
                    else if (!isFolder.Value && !entry.Value.IsDirectory)
                    {
                        continue;
                    }
                }

                if (entry.Key.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }
        
        public KeyValuePair<string, WIN32_FIND_DATA>? GetFileSystemEntryByName(string name, bool? isFolder)
        {
            foreach (KeyValuePair<string, WIN32_FIND_DATA> entry in FileSystemChildren)
            {
                if (isFolder.HasValue)
                {
                    if (isFolder.Value && entry.Value.IsDirectory)
                    {
                        continue;
                    }
                    else if (!isFolder.Value && !entry.Value.IsDirectory)
                    {
                        continue;
                    }
                }

                if (System.IO.Path.GetFileName(entry.Key).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        public bool ContainsFile(string name)
        {
            return GetFileSystemEntryByName(name, false) != null;
        }

        public bool ContainsFolder(string name)
        {
            return GetFileSystemEntryByName(name, true) != null;
        }
    }

    /// <summary>
    /// This is an EventArgs object used before we begin resolving a Path into a BaseItem
    /// File system children have not been collected yet, but consuming events will
    /// have a chance to cancel resolution based on the Path, Parent and FileAttributes
    /// </summary>
    public class PreBeginResolveEventArgs : EventArgs
    {
        public string Path { get; set; }
        public Folder Parent { get; set; }

        public bool Cancel { get; set; }

        public FileAttributes FileAttributes { get { return FileData.dwFileAttributes; } }
        public WIN32_FIND_DATA FileData { get; set; }

        public bool IsFolder
        {
            get
            {
                return FileAttributes.HasFlag(FileAttributes.Directory);
            }
        }

        public bool IsHidden
        {
            get
            {
                return FileAttributes.HasFlag(FileAttributes.Hidden);
            }
        }

        public bool IsSystemFile
        {
            get
            {
                return FileAttributes.HasFlag(FileAttributes.System);
            }
        }

        public VirtualFolder VirtualFolder
        {
            get
            {
                if (Parent != null)
                {
                    return Parent.VirtualFolder;
                }

                return null;
            }
        }

        public string VirtualFolderCollectionType
        {
            get
            {
                VirtualFolder vf = VirtualFolder;

                if (vf == null)
                {
                    return null;
                }

                return vf.CollectionType;
            }
        }
    }
}
