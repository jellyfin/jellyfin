using System;
using System.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// This is an EventArgs object used when resolving a Path into a BaseItem
    /// </summary>
    public class ItemResolveEventArgs : PreBeginResolveEventArgs
    {
        public WIN32_FIND_DATA[] FileSystemChildren { get; set; }

        public WIN32_FIND_DATA? GetFileSystemEntry(string path)
        {
            for (int i = 0; i < FileSystemChildren.Length; i++)
            {
                WIN32_FIND_DATA entry = FileSystemChildren[i];

                if (entry.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
           
            return null;
        }

        public bool ContainsFile(string name)
        {
            for (int i = 0; i < FileSystemChildren.Length; i++)
            {
                if (FileSystemChildren[i].cFileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
