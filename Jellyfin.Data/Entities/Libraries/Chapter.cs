using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a chapter.
    /// </summary>
    public class Chapter : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Chapter"/> class.
        /// </summary>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        /// <param name="startTime">The start time for this chapter.</param>
        public Chapter(string language, long startTime)
        {
            ArgumentException.ThrowIfNullOrEmpty(language);

            Language = language;
            StartTime = startTime;
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
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <remarks>
        /// Required, Min length = 3, Max length = 3
        /// ISO-639-3 3-character language codes.
        /// </remarks>
        [MinLength(3)]
        [MaxLength(3)]
        [StringLength(3)]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public long StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public long? EndTime { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
