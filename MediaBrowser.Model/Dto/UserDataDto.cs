#nullable disable
using System;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class UserDataDto extends UserItemDataDto to allow nullable members.
    /// This change allow us to implement the new /Users/{UserId}/Items/{ItemId}/UserData endpoint.
    /// This object allows the requestor to update all or specific user data fields without altering the non-nullable members state.
    /// </summary>
    public class UserDataDto : UserItemDataDto
    {
        /// <summary>
        /// Gets or sets the playback position ticks.
        /// </summary>
        /// <value>The playback position ticks.</value>
        public new long? PlaybackPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play count.
        /// </summary>
        /// <value>The play count.</value>
        public new int? PlayCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>true</c> if this instance is favorite; otherwise, <c>false</c>.</value>
        public new bool? IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemDataDto" /> is likes.
        /// </summary>
        /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
        public new bool? Likes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemDataDto" /> is played.
        /// </summary>
        /// <value><c>true</c> if played; otherwise, <c>false</c>.</value>
        public new bool? Played { get; set; }
    }
}
