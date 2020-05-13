using System;
using System.ComponentModel.DataAnnotations;

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

        public int Id { get; protected set; }

        [Required]
        public string Path { get; set; }

        [Required]
        public DateTime LastModified { get; set; }
    }
}
