using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Controller.Persistence
{
    public class MediaStreamQuery
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public MediaStreamType? Type { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public int? Index { get; set; }

        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public Guid ItemId { get; set; }
    }
}
