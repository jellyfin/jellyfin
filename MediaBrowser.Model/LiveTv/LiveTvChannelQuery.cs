#nullable disable
#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ChannelQuery.
    /// </summary>
    public class LiveTvChannelQuery
    {
        public LiveTvChannelQuery()
        {
            EnableUserData = true;
            SortBy = Array.Empty<ItemSortBy>();
        }

        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType? ChannelType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
        public bool? IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is liked.
        /// </summary>
        /// <value><c>null</c> if [is liked] contains no value, <c>true</c> if [is liked]; otherwise, <c>false</c>.</value>
        public bool? IsLiked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is disliked.
        /// </summary>
        /// <value><c>null</c> if [is disliked] contains no value, <c>true</c> if [is disliked]; otherwise, <c>false</c>.</value>
        public bool? IsDisliked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable favorite sorting].
        /// </summary>
        /// <value><c>true</c> if [enable favorite sorting]; otherwise, <c>false</c>.</value>
        public bool EnableFavoriteSorting { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the start index. Used for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to return.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [add current program].
        /// </summary>
        /// <value><c>true</c> if [add current program]; otherwise, <c>false</c>.</value>
        public bool AddCurrentProgram { get; set; }

        public bool EnableUserData { get; set; }

        /// <summary>
        /// Gets or sets a value whether to return news or not.
        /// </summary>
        /// <remarks>If set to <c>null</c>, all programs will be returned.</remarks>
        public bool? IsNews { get; set; }

        /// <summary>
        /// Gets or sets a value whether to return movies or not.
        /// </summary>
        /// <remarks>If set to <c>null</c>, all programs will be returned.</remarks>
        public bool? IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>null</c> if [is kids] contains no value, <c>true</c> if [is kids]; otherwise, <c>false</c>.</value>
        public bool? IsKids { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>null</c> if [is sports] contains no value, <c>true</c> if [is sports]; otherwise, <c>false</c>.</value>
        public bool? IsSports { get; set; }

        public bool? IsSeries { get; set; }

        public ItemSortBy[] SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort order to return results with.
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder? SortOrder { get; set; }
    }
}
