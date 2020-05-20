using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public class ImageInfo
    {
        public ImageInfo(string path, int width, int height)
        {
            Path = path;
            Width = width;
            Height = height;
            LastModified = DateTime.UtcNow;
        }

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        [Required]
        public string Path { get; set; }

        [Required]
        public int Width { get; set; }

        [Required]
        public int Height { get; set; }

        [Required]
        public DateTime LastModified { get; set; }
    }
}
