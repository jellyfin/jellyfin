using System;
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
                var folder = item as Folder;

                if (folder != null)
                {
                    var foundItem = folder.FindById(id);

                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
                else if (item.Id == id)
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
                var folder = item as Folder;

                if (folder != null)
                {
                    var foundItem = folder.FindByPath(path);

                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
                else if (item.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
