#nullable disable

#pragma warning disable CS1591

using System;
using MediaBrowser.Controller.Entities.Movies;

namespace MediaBrowser.Controller.Collections
{
    public class CollectionCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        /// <value>The collection.</value>
        public BoxSet Collection { get; set; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public CollectionCreationOptions Options { get; set; }
    }
}
