#pragma warning disable CA2227

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity holding the metadata for a movie.
    /// </summary>
    public class MovieMetadata : ItemMetadata, IHasCompanies
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the movie.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        /// <param name="movie">The movie.</param>
        public MovieMetadata(string title, string language, Movie movie) : base(title, language)
        {
            Studios = new HashSet<Company>();

            movie.MovieMetadata.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieMetadata"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected MovieMetadata()
        {
        }

        /// <summary>
        /// Gets or sets the outline.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Outline { get; set; }

        /// <summary>
        /// Gets or sets the tagline.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Tagline { get; set; }

        /// <summary>
        /// Gets or sets the plot.
        /// </summary>
        /// <remarks>
        /// Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Plot { get; set; }

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        /// <remarks>
        /// Max length = 2.
        /// </remarks>
        [MaxLength(2)]
        [StringLength(2)]
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets the studios that produced this movie.
        /// </summary>
        public virtual ICollection<Company> Studios { get; protected set; }

        /// <inheritdoc />
        [NotMapped]
        public ICollection<Company> Companies => Studios;
    }
}
