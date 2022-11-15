#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Querying
{
    public class NextUpQuery
    {
        public NextUpQuery()
        {
            EnableImageTypes = Array.Empty<ImageType>();
            EnableTotalRecordCount = true;
            DisableFirstEpisode = false;
            NextUpDateCutoff = DateTime.MinValue;
            EnableRewatching = false;
        }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the parent identifier.
        /// </summary>
        /// <value>The parent identifier.</value>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Gets or sets the series id.
        /// </summary>
        /// <value>The series id.</value>
        public Guid? SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the start index. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to return.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// gets or sets the fields to return within the items, in addition to basic information.
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable images].
        /// </summary>
        /// <value><c>null</c> if [enable images] contains no value, <c>true</c> if [enable images]; otherwise, <c>false</c>.</value>
        public bool? EnableImages { get; set; }

        /// <summary>
        /// Gets or sets the image type limit.
        /// </summary>
        /// <value>The image type limit.</value>
        public int? ImageTypeLimit { get; set; }

        /// <summary>
        /// Gets or sets the enable image types.
        /// </summary>
        /// <value>The enable image types.</value>
        public ImageType[] EnableImageTypes { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether do disable sending first episode as next up.
        /// </summary>
        public bool DisableFirstEpisode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the oldest date for a show to appear in Next Up.
        /// </summary>
        public DateTime NextUpDateCutoff { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether getting rewatching next up list.
        /// </summary>
        public bool EnableRewatching { get; set; }
    }
}
