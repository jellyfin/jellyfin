#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Channels
{
    public class ChannelFeatures
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can search.
        /// </summary>
        /// <value><c>true</c> if this instance can search; otherwise, <c>false</c>.</value>
        public bool CanSearch { get; set; }

        /// <summary>
        /// Gets or sets the media types.
        /// </summary>
        /// <value>The media types.</value>
        public ChannelMediaType[] MediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the content types.
        /// </summary>
        /// <value>The content types.</value>
        public ChannelMediaContentType[] ContentTypes { get; set; }

        /// <summary>
        /// Represents the maximum number of records the channel allows retrieving at a time
        /// </summary>
        public int? MaxPageSize { get; set; }

        /// <summary>
        /// Gets or sets the automatic refresh levels.
        /// </summary>
        /// <value>The automatic refresh levels.</value>
        public int? AutoRefreshLevels { get; set; }

        /// <summary>
        /// Gets or sets the default sort orders.
        /// </summary>
        /// <value>The default sort orders.</value>
        public ChannelItemSortField[] DefaultSortFields { get; set; }

        /// <summary>
        /// Indicates if a sort ascending/descending toggle is supported or not.
        /// </summary>
        public bool SupportsSortOrderToggle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports latest media].
        /// </summary>
        /// <value><c>true</c> if [supports latest media]; otherwise, <c>false</c>.</value>
        public bool SupportsLatestMedia { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can filter.
        /// </summary>
        /// <value><c>true</c> if this instance can filter; otherwise, <c>false</c>.</value>
        public bool CanFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports content downloading].
        /// </summary>
        /// <value><c>true</c> if [supports content downloading]; otherwise, <c>false</c>.</value>
        public bool SupportsContentDownloading { get; set; }

        public ChannelFeatures()
        {
            MediaTypes = Array.Empty<ChannelMediaType>();
            ContentTypes = Array.Empty<ChannelMediaContentType>();
            DefaultSortFields = Array.Empty<ChannelItemSortField>();
        }
    }
}
