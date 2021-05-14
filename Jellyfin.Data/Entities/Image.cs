using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a single image.
    /// </summary>
    public class Image : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="path">The path of the image.</param>
        /// <param name="type">The image type.</param>
        public Image(string path, ImageType type = ImageType.Primary)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
            Type = type;
            AddedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="path">The path of the image.</param>
        /// <param name="type">The image type.</param>
        /// <param name="lastModifiedDate">The last modification date.</param>
        public Image(string path, DateTime lastModifiedDate, ImageType type = ImageType.Primary) : this(path, type)
        {
            LastModifiedDate = lastModifiedDate;
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
        /// Gets the path.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Path { get; private set; }

        /// <summary>
        /// Gets the image type.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public ImageType Type { get; private set; }

        /// <summary>
        /// Gets the blurhash string.
        /// </summary>
        public string? Blurhash { get; private set; }

        /// <summary>
        /// Gets the date last modified.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime LastModifiedDate { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the date when the image was added to the database.
        /// </summary>
        public DateTime AddedDate { get; private set; }

        /// <summary>
        /// Gets the creation date of the file.
        /// </summary>
        public DateTime? FileCreationDate { get; private set; }

        /// <summary>
        /// Gets the modification date of the file.
        /// </summary>
        public DateTime? FileModificationDate { get; private set; }

        /// <inheritdoc/>
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            LastModifiedDate = DateTime.UtcNow;
            RowVersion++;
        }
    }
}
