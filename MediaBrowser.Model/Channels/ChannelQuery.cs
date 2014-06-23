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

        /// <summary>
        /// Gets or sets a value indicating whether [supports latest items].
        /// </summary>
        /// <value><c>true</c> if [supports latest items]; otherwise, <c>false</c>.</value>
        public bool? SupportsLatestItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
        public bool? IsFavorite { get; set; }
    }

    public class AllChannelMediaQuery
    {
        /// <summary>
        /// Gets or sets the channel ids.
        /// </summary>
        /// <value>The channel ids.</value>
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
