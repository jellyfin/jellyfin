using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Events
{
    /// <summary>
    /// This is an EventArgs object used when resolving a Path into a BaseItem
    /// </summary>
    public class ItemResolveEventArgs : PreBeginResolveEventArgs
    {
        public IEnumerable<KeyValuePair<string, FileAttributes>> FileSystemChildren { get; set; }

        public KeyValuePair<string, FileAttributes>? GetFolderByName(string name)
        {
            foreach (KeyValuePair<string, FileAttributes> entry in FileSystemChildren)
            {
                if (!entry.Value.HasFlag(FileAttributes.Directory))
                {
                    continue;
                }

                if (System.IO.Path.GetFileName(entry.Key).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        public KeyValuePair<string, FileAttributes>? GetFileByName(string name)
        {
            foreach (KeyValuePair<string, FileAttributes> entry in FileSystemChildren)
            {
                if (entry.Value.HasFlag(FileAttributes.Directory))
                {
                    continue;
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
            return GetFileByName(name) != null;
        }

        public bool ContainsFolder(string name)
        {
            return GetFolderByName(name) != null;
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

        public FileAttributes FileAttributes { get; set; }

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
