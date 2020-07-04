using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public class UserItemData
    {
        public const double MinLikeValue = 6.5;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public Guid ItemId { get; set; }

        public bool IsPlayed { get; set; }

        public int PlayCount { get; set; }

        public long PlaybackPositionTicks { get; set; }

        public DateTime? LastPlayedDate { get; set; }

        public int? VideoStreamIndex { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public bool IsFavorite { get; set; }

        public float? Rating { get; set; }

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
