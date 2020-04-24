using System;
using MediaBrowser.Controller.Entities.Movies;

namespace MediaBrowser.Controller.Collections
{
    /// <summary>
    /// Object storing the arguments for the <see cref="ICollectionManager.CollectionCreated">CollectionCreated</see> event.
    /// </summary>
    public class CollectionCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        public BoxSet Collection { get; set; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public CollectionCreationOptions Options { get; set; }
    }
}
