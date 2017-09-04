using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Channels
{
    public class ChannelItemQuery
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the category identifier.
        /// </summary>
        /// <value>The category identifier.</value>
        public string FolderId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        public ItemFilter[] Filters { get; set; }
        public ItemFields[] Fields { get; set; }
        public Tuple<string, SortOrder>[] OrderBy { get; set; }

        public ChannelItemQuery()
        {
            Filters = new ItemFilter[] { };
            Fields = new ItemFields[] { };
            OrderBy = new Tuple<string, SortOrder>[] { };
        }
    }

}