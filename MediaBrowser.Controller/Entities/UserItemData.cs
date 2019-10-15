using System;
using System.Text.Json.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class UserItemData
    /// </summary>
    public class UserItemData
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// The _rating
        /// </summary>
        private double? _rating;
        /// <summary>
        /// Gets or sets the users 0-10 rating
        /// </summary>
        /// <value>The rating.</value>
        /// <exception cref="ArgumentOutOfRangeException">Rating;A 0 to 10 rating is required for UserItemData.</exception>
        public double? Rating
        {
            get => _rating;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0 || value.Value > 10)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "A 0 to 10 rating is required for UserItemData.");
                    }
                }

                _rating = value;
            }
        }

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
        /// Gets or sets the last played date.
        /// </summary>
        /// <value>The last played date.</value>
        public DateTime? LastPlayedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserItemData" /> is played.
        /// </summary>
        /// <value><c>true</c> if played; otherwise, <c>false</c>.</value>
        public bool Played { get; set; }
        /// <summary>
        /// Gets or sets the index of the audio stream.
        /// </summary>
        /// <value>The index of the audio stream.</value>
        public int? AudioStreamIndex { get; set; }
        /// <summary>
        /// Gets or sets the index of the subtitle stream.
        /// </summary>
        /// <value>The index of the subtitle stream.</value>
        public int? SubtitleStreamIndex { get; set; }

        public const double MinLikeValue = 6.5;

        /// <summary>
        /// This is an interpreted property to indicate likes or dislikes
        /// This should never be serialized.
        /// </summary>
        /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool? Likes
        {
            get
            {
                if (Rating != null)
                {
                    return Rating >= MinLikeValue;
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
