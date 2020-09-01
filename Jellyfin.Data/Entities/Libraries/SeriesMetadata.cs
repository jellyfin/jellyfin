#pragma warning disable CA2227

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing series metadata.
    /// </summary>
    public class SeriesMetadata : ItemMetadata, IHasCompanies
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        /// <param name="series">The series.</param>
        public SeriesMetadata(string title, string language, Series series) : base(title, language)
        {
            if (series == null)
            {
                throw new ArgumentNullException(nameof(series));
            }

            series.SeriesMetadata.Add(this);

            Networks = new HashSet<Company>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesMetadata"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected SeriesMetadata()
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
        /// Gets or sets the plot.
        /// </summary>
        /// <remarks>
        /// Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Plot { get; set; }

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
        /// Gets or sets the country code.
        /// </summary>
        /// <remarks>
        /// Max length = 2.
        /// </remarks>
        [MaxLength(2)]
        [StringLength(2)]
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the networks.
        /// </summary>
        public virtual ICollection<Company> Networks { get; protected set; }

        /// <inheritdoc />
        [NotMapped]
        public ICollection<Company> Companies => Networks;
    }
}
