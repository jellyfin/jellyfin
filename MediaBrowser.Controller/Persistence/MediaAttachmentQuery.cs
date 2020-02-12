using System;

namespace MediaBrowser.Controller.Persistence
{
    public class MediaAttachmentQuery
    {
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
