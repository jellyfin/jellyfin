using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public class UserMediaMetadata
    {
        public const double MinLikeValue = 6.5;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public Guid ItemId { get; set; }

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
