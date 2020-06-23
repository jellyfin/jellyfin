using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public class ImageInfo
    {
        public ImageInfo(string path)
        {
            Path = path;
            LastModified = DateTime.UtcNow;
        }

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        public Guid? UserId { get; protected set; }

        [Required]
        [MaxLength(512)]
        [StringLength(512)]
        public string Path { get; set; }

        [Required]
        public DateTime LastModified { get; set; }
    }
}
