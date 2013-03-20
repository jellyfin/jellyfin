using System;
using System.ComponentModel;
using ProtoBuf;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class UserItemDataDto
    /// </summary>
    [ProtoContract]
    public class UserItemDataDto : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        /// <value>The rating.</value>
        [ProtoMember(1)]
        public float? Rating { get; set; }

        /// <summary>
        /// Gets or sets the playback position ticks.
        /// </summary>
        /// <value>The playback position ticks.</value>
        [ProtoMember(2)]
        public long PlaybackPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play count.
        /// </summary>
        /// <value>The play count.</value>
        [ProtoMember(3)]
        public int PlayCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>true</c> if this instance is favorite; otherwise, <c>false</c>.</value>
        [ProtoMember(4)]
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemDataDto" /> is likes.
        /// </summary>
        /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
        [ProtoMember(5)]
        public bool? Likes { get; set; }

        /// <summary>
        /// Gets or sets the last played date.
        /// </summary>
        /// <value>The last played date.</value>
        [ProtoMember(6)]
        public DateTime? LastPlayedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemDataDto" /> is played.
        /// </summary>
        /// <value><c>true</c> if played; otherwise, <c>false</c>.</value>
        [ProtoMember(7)]
        public bool Played { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
