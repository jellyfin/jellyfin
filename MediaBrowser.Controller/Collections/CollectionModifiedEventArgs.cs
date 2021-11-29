#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

namespace MediaBrowser.Controller.Collections
{
    public class CollectionModifiedEventArgs : EventArgs
    {
        public CollectionModifiedEventArgs(BoxSet collection, IReadOnlyCollection<BaseItem> itemsChanged)
        {
            Collection = collection;
            ItemsChanged = itemsChanged;
        }

        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        /// <value>The collection.</value>
        public BoxSet Collection { get; set; }

        /// <summary>
        /// Gets or sets the items changed.
        /// </summary>
        /// <value>The items changed.</value>
        public IReadOnlyCollection<BaseItem> ItemsChanged { get; set; }
    }
}
