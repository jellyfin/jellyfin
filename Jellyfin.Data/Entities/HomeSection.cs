using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    public class HomeSection
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        public int DisplayPreferencesId { get; set; }

        public int Order { get; set; }

        public HomeSectionType Type { get; set; }
    }
}
