using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public class UserWatchState
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public Guid ItemId { get; set; }

        public bool IsPlayed { get; set; }

        public int PlayCount { get; set; }

        public long PlaybackPositionTicks { get; set; }

        public DateTime? LastPlayedDate { get; set; }
    }
}
