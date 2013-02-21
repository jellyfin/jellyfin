using ProtoBuf;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class UserItemData
    /// </summary>
    [ProtoContract]
    public class UserItemData
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ProtoMember(1)]
        public Guid UserId { get; set; }

        /// <summary>
        /// The _rating
        /// </summary>
        private float? _rating;
        /// <summary>
        /// Gets or sets the users 0-10 rating
        /// </summary>
        /// <value>The rating.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">A 0-10 rating is required for UserItemData.</exception>
        /// <exception cref="System.InvalidOperationException">A 0-10 rating is required for UserItemData.</exception>
        [ProtoMember(2)]
        public float? Rating
        {
            get
            {
                return _rating;
            }
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0 || value.Value > 10)
                    {
                        throw new ArgumentOutOfRangeException("A 0-10 rating is required for UserItemData.");
                    }
                }

                _rating = value;
            }
        }

        /// <summary>
        /// Gets or sets the playback position ticks.
        /// </summary>
        /// <value>The playback position ticks.</value>
        [ProtoMember(3)]
        public long PlaybackPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play count.
        /// </summary>
        /// <value>The play count.</value>
        [ProtoMember(4)]
        public int PlayCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>true</c> if this instance is favorite; otherwise, <c>false</c>.</value>
        [ProtoMember(5)]
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets the last played date.
        /// </summary>
        /// <value>The last played date.</value>
        [ProtoMember(6)]
        public DateTime? LastPlayedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemData" /> is played.
        /// </summary>
        /// <value><c>true</c> if played; otherwise, <c>false</c>.</value>
        [ProtoMember(7)]
        public bool Played { get; set; }

        /// <summary>
        /// This is an interpreted property to indicate likes or dislikes
        /// This should never be serialized.
        /// </summary>
        /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool? Likes
        {
            get
            {
                if (Rating != null)
                {
                    return Rating >= 6.5;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Rating = value.Value ? 10 : 1;
                }
                else
                {
                    Rating = null;
                }
            }
        }
    }
}
