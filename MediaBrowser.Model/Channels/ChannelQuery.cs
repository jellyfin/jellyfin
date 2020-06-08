#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Channels
{
    public class ChannelQuery
    {
        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        public bool? EnableImages { get; set; }

        public int? ImageTypeLimit { get; set; }

        public ImageType[] EnableImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }

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

        public bool? SupportsMediaDeletion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
        public bool? IsFavorite { get; set; }

        public bool? IsRecordingsFolder { get; set; }

        public bool RefreshLatestChannelItems { get; set; }
    }
}
