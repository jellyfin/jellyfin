#pragma warning disable CA2227

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An abstract class that holds metadata.
    /// </summary>
    public abstract class ItemMetadata : IHasArtwork, IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        protected ItemMetadata(string title, string language)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException(nameof(language));
            }

            Title = title;
            Language = language;
            DateAdded = DateTime.UtcNow;
            DateModified = DateAdded;

            PersonRoles = new HashSet<PersonRole>();
            Genres = new HashSet<Genre>();
            Artwork = new HashSet<Artwork>();
            Ratings = new HashSet<Rating>();
            Sources = new HashSet<MetadataProviderId>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemMetadata"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to being abstract.
        /// </remarks>
        protected ItemMetadata()
        {
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 1024.
        /// </remarks>
        [Required]
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the original title.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string OriginalTitle { get; set; }

        /// <summary>
        /// Gets or sets the sort title.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string SortTitle { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <remarks>
        /// Required, Min length = 3, Max length = 3.
        /// ISO-639-3 3-character language codes.
        /// </remarks>
        [Required]
        [MinLength(3)]
        [MaxLength(3)]
        [StringLength(3)]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public DateTimeOffset? ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the date added.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime DateAdded { get; protected set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets the row version.
        /// </summary>
        /// <remarks>
        /// Required, ConcurrencyToken.
        /// </remarks>
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the person roles for this item.
        /// </summary>
        public virtual ICollection<PersonRole> PersonRoles { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the genres for this item.
        /// </summary>
        public virtual ICollection<Genre> Genres { get; protected set; }

        /// <inheritdoc />
        public virtual ICollection<Artwork> Artwork { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the ratings for this item.
        /// </summary>
        public virtual ICollection<Rating> Ratings { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the metadata sources for this item.
        /// </summary>
        public virtual ICollection<MetadataProviderId> Sources { get; protected set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
