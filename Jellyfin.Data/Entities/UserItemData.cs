using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a user's metadata, stream settings, and watch state for a particular item.
    /// </summary>
    public class UserItemData
    {
        /// <summary>
        /// The minimum rating value to be considered "liked".
        /// </summary>
        public const double MinLikeValue = 6.5;

        /// <summary>
        /// Gets or sets the id of the user item data.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the id of the user this entity is attached to.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id of the item this entity is attached to.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this media is played.
        /// </summary>
        public bool IsPlayed { get; set; }

        /// <summary>
        /// Gets or sets the number of times this media has been played.
        /// </summary>
        public int PlayCount { get; set; }

        /// <summary>
        /// Gets or sets the playback position ticks.
        /// </summary>
        public long PlaybackPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the last played date.
        /// </summary>
        public DateTime? LastPlayedDate { get; set; }

        /// <summary>
        /// Gets or sets the video stream index.
        /// </summary>
        public int? VideoStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the audio stream index.
        /// </summary>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the subtitle stream index.
        /// </summary>
        public int? SubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this media is favorited by the user.
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        public float? Rating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user likes this media.
        /// </summary>
        [NotMapped]
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
