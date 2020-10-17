#pragma warning disable CA2227

using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a season.
    /// </summary>
    public class Season : LibraryItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Season"/> class.
        /// </summary>
        /// <param name="series">The series.</param>
        public Season(Series series)
        {
            if (series == null)
            {
                throw new ArgumentNullException(nameof(series));
            }

            series.Seasons.Add(this);

            Episodes = new HashSet<Episode>();
            SeasonMetadata = new HashSet<SeasonMetadata>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Season"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Season()
        {
        }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the season metadata.
        /// </summary>
        public virtual ICollection<SeasonMetadata> SeasonMetadata { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the number of episodes.
        /// </summary>
        public virtual ICollection<Episode> Episodes { get; protected set; }
    }
}
