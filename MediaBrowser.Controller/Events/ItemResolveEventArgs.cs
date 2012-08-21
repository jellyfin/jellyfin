using System;
using System.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Events
{
    /// <summary>
    /// This is an EventArgs object used when resolving a Path into a BaseItem
    /// </summary>
    public class ItemResolveEventArgs : PreBeginResolveEventArgs
    {
        public LazyFileInfo[] FileSystemChildren { get; set; }

        public LazyFileInfo? GetFileSystemEntry(string path, bool? isFolder = null)
        {
            for (int i = 0; i < FileSystemChildren.Length; i++)
            {
                LazyFileInfo entry = FileSystemChildren[i];

                if (entry.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    if (isFolder.HasValue)
                    {
                        if (isFolder.Value && !entry.FileInfo.IsDirectory)
                        {
                            continue;
                        }
                        else if (!isFolder.Value && entry.FileInfo.IsDirectory)
                        {
                            continue;
                        }
                    }
                    
                    return entry;
                }
            }

            return null;
        }

        public LazyFileInfo? GetFileSystemEntryByName(string name, bool? isFolder = null)
        {
            for (int i = 0; i < FileSystemChildren.Length; i++)
            {
                LazyFileInfo entry = FileSystemChildren[i];

                if (System.IO.Path.GetFileName(entry.Path).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (isFolder.HasValue)
                    {
                        if (isFolder.Value && !entry.FileInfo.IsDirectory)
                        {
                            continue;
                        }
                        else if (!isFolder.Value && entry.FileInfo.IsDirectory)
                        {
                            continue;
                        }
                    }

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
