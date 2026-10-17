using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// The result of a channel item query.
    /// </summary>
    public class ChannelItemResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelItemResult"/> class.
        /// </summary>
        public ChannelItemResult()
        {
            Items = Array.Empty<ChannelItemInfo>();
        }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        public IReadOnlyList<ChannelItemInfo> Items { get; set; }

        /// <summary>
        /// Gets or sets the total record count.
        /// </summary>
        public int? TotalRecordCount { get; set; }
    }
}
