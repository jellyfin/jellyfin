#nullable disable
using System;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class UserItemDataDto.
    /// </summary>
    public class UserItemDataDto
    {
        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        /// <value>The rating.</value>
        public double? Rating { get; set; }

        /// <summary>
        /// Gets or sets the played percentage.
        /// </summary>
        /// <value>The played percentage.</value>
        public double? PlayedPercentage { get; set; }

        /// <summary>
        /// Gets or sets the unplayed item count.
        /// </summary>
        /// <value>The unplayed item count.</value>
        public int? UnplayedItemCount { get; set; }

        /// <summary>
        /// Gets or sets the playback position ticks.
        /// </summary>
        /// <value>The playback position ticks.</value>
        public long PlaybackPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play count.
        /// </summary>
        /// <value>The play count.</value>
        public int PlayCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>true</c> if this instance is favorite; otherwise, <c>false</c>.</value>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemDataDto" /> is likes.
        /// </summary>
        /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
        public bool? Likes { get; set; }

        /// <summary>
        /// Gets or sets the last played date.
        /// </summary>
        /// <value>The last played date.</value>
        public DateTime? LastPlayedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemDataDto" /> is played.
        /// </summary>
        /// <value><c>true</c> if played; otherwise, <c>false</c>.</value>
        public bool Played { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string ItemId { get; set; }
    }
}
