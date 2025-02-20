using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing an image.
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInfo"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public ImageInfo(string path)
        {
            Path = path;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public Guid? UserId { get; private set; }

        /// <summary>
        /// Gets or sets the path of the image.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [MaxLength(512)]
        [StringLength(512)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the date last modified.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime LastModified { get; set; }
    }
}
