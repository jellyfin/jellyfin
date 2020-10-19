#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Querying
{
    public class LatestItemsQuery
    {
        public LatestItemsQuery()
        {
            EnableImageTypes = Array.Empty<ImageType>();
        }

        /// <summary>
        /// The user to localize search results for.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        public Guid ParentId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information.
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        /// <summary>
        /// Gets or sets the include item types.
        /// </summary>
        /// <value>The include item types.</value>
        public string[] IncludeItemTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is played.
        /// </summary>
        /// <value><c>null</c> if [is played] contains no value, <c>true</c> if [is played]; otherwise, <c>false</c>.</value>
        public bool? IsPlayed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [group items].
        /// </summary>
        /// <value><c>true</c> if [group items]; otherwise, <c>false</c>.</value>
        public bool GroupItems { get; set; }

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
    }
}
