#nullable disable

#pragma warning disable CA1002, CA2227, CS1591

using System.Collections.Generic;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Controller.Channels
{
    public class InternalChannelFeatures
    {
        public InternalChannelFeatures()
        {
            MediaTypes = new List<ChannelMediaType>();
            ContentTypes = new List<ChannelMediaContentType>();

            DefaultSortFields = new List<ChannelItemSortField>();
        }

        /// <summary>
        /// Gets or sets the media types.
        /// </summary>
        /// <value>The media types.</value>
        public List<ChannelMediaType> MediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the content types.
        /// </summary>
        /// <value>The content types.</value>
        public List<ChannelMediaContentType> ContentTypes { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of records the channel allows retrieving at a time.
        /// </summary>
        public int? MaxPageSize { get; set; }

        /// <summary>
        /// Gets or sets the default sort orders.
        /// </summary>
        /// <value>The default sort orders.</value>
        public List<ChannelItemSortField> DefaultSortFields { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a sort ascending/descending toggle is supported or not.
        /// </summary>
        public bool SupportsSortOrderToggle { get; set; }

        /// <summary>
        /// Gets or sets the automatic refresh levels.
        /// </summary>
        /// <value>The automatic refresh levels.</value>
        public int? AutoRefreshLevels { get; set; }

        /// <summary>
        /// Gets or sets the daily download limit.
        /// </summary>
        /// <value>The daily download limit.</value>
        public int? DailyDownloadLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports downloading].
        /// </summary>
        /// <value><c>true</c> if [supports downloading]; otherwise, <c>false</c>.</value>
        public bool SupportsContentDownloading { get; set; }
    }
}
