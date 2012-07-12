using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Events
{
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

    public class PreBeginResolveEventArgs : EventArgs
    {
        public string Path { get; set; }
        public BaseItem Parent { get; set; }

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

    }
}
