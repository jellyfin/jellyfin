#pragma warning disable CA2227

using System;
using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing an episode.
    /// </summary>
    public class Episode : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Episode"/> class.
        /// </summary>
        /// <param name="season">The season.</param>
        public Episode(Season season)
        {
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            season.Episodes.Add(this);

            Releases = new HashSet<Release>();
            EpisodeMetadata = new HashSet<EpisodeMetadata>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Episode"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Episode()
        {
        }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        public int? EpisodeNumber { get; set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the metadata for this episode.
        /// </summary>
        public virtual ICollection<EpisodeMetadata> EpisodeMetadata { get; protected set; }
    }
}
