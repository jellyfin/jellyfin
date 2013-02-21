using System.Collections.Concurrent;
using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class ChildrenChangedEventArgs
    /// </summary>
    public class ChildrenChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the folder.
        /// </summary>
        /// <value>The folder.</value>
        public Folder Folder { get; set; }
        /// <summary>
        /// Gets or sets the items added.
        /// </summary>
        /// <value>The items added.</value>
        public ConcurrentBag<BaseItem> ItemsAdded { get; set; }
        /// <summary>
        /// Gets or sets the items removed.
        /// </summary>
        /// <value>The items removed.</value>
        public List<BaseItem> ItemsRemoved { get; set; }
        /// <summary>
        /// Gets or sets the items updated.
        /// </summary>
        /// <value>The items updated.</value>
        public ConcurrentBag<BaseItem> ItemsUpdated { get; set; }

        /// <summary>
        /// Create the args and set the folder property
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ChildrenChangedEventArgs(Folder folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException();
            }

            //init the folder property
            Folder = folder;
            //init the list
            ItemsAdded = new ConcurrentBag<BaseItem>();
            ItemsRemoved = new List<BaseItem>();
            ItemsUpdated = new ConcurrentBag<BaseItem>();
        }

        /// <summary>
        /// Adds the new item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddNewItem(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }
            
            ItemsAdded.Add(item);
        }

        /// <summary>
        /// Adds the updated item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddUpdatedItem(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }
            
            ItemsUpdated.Add(item);
        }

        /// <summary>
        /// Adds the removed item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddRemovedItem(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            ItemsRemoved.Add(item);
        }

        /// <summary>
        /// Lists the has change.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ListHasChange(List<BaseItem> list)
        {
            return list != null && list.Count > 0;
        }

        /// <summary>
        /// Lists the has change.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ListHasChange(ConcurrentBag<BaseItem> list)
        {
            return list != null && !list.IsEmpty;
        }
        
        /// <summary>
        /// Gets a value indicating whether this instance has change.
        /// </summary>
        /// <value><c>true</c> if this instance has change; otherwise, <c>false</c>.</value>
        public bool HasChange
        {
            get { return HasAddOrRemoveChange || ListHasChange(ItemsUpdated); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has add or remove change.
        /// </summary>
        /// <value><c>true</c> if this instance has add or remove change; otherwise, <c>false</c>.</value>
        public bool HasAddOrRemoveChange
        {
            get { return ListHasChange(ItemsAdded) || ListHasChange(ItemsRemoved); }
        }
    }
}
