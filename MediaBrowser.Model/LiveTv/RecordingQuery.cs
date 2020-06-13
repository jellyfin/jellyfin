#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class RecordingQuery.
    /// </summary>
    public class RecordingQuery
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

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
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public RecordingStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is in progress.
        /// </summary>
        /// <value><c>null</c> if [is in progress] contains no value, <c>true</c> if [is in progress]; otherwise, <c>false</c>.</value>
        public bool? IsInProgress { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        /// <value>The series timer identifier.</value>
        public string SeriesTimerId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }
        public bool? EnableImages { get; set; }
        public bool? IsLibraryItem { get; set; }
        public bool? IsNews { get; set; }
        public bool? IsMovie { get; set; }
        public bool? IsSeries { get; set; }
        public bool? IsKids { get; set; }
        public bool? IsSports { get; set; }
        public int? ImageTypeLimit { get; set; }
        public ImageType[] EnableImageTypes { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        public RecordingQuery()
        {
            EnableTotalRecordCount = true;
        }
    }
}
