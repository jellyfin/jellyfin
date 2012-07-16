using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Entities
{
    public class Folder : BaseItem
    {
        public bool IsRoot { get; set; }

        public bool IsVirtualFolder
        {
            get
            {
                return Parent != null && Parent.IsRoot;
            }
        }

        [IgnoreDataMember]
        public BaseItem[] Children { get; set; }

        [IgnoreDataMember]
        public IEnumerable<Folder> FolderChildren { get { return Children.OfType<Folder>(); } }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        public BaseItem FindById(Guid id)
        {
            if (Id == id)
            {
                return this;
            }

            foreach (BaseItem item in Children)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            foreach (Folder folder in FolderChildren)
            {
                BaseItem item = folder.FindById(id);

                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds an item by path, recursively
        /// </summary>
        public BaseItem FindByPath(string path)
        {
            if (Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }

            foreach (BaseItem item in Children)
            {
                if (item.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            foreach (Folder folder in FolderChildren)
            {
                BaseItem item = folder.FindByPath(path);

                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
