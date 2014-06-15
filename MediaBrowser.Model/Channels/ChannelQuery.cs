using MediaBrowser.Model.Querying;
using System.Collections.Generic;

namespace MediaBrowser.Model.Channels
{
    public class ChannelQuery
    {
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
    }

    public class AllChannelMediaQuery
    {
        public string[] ChannelIds { get; set; }

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

        /// <summary>
        /// Gets or sets the content types.
        /// </summary>
        /// <value>The content types.</value>
        public ChannelMediaContentType[] ContentTypes { get; set; }

        public AllChannelMediaQuery()
        {
            ChannelIds = new string[] { };

            ContentTypes = new ChannelMediaContentType[] { };

            Filters = new ItemFilter[] { };
            Fields = new List<ItemFields>();
        }

        public ItemFilter[] Filters { get; set; }
        public List<ItemFields> Fields { get; set; }
    }

}
