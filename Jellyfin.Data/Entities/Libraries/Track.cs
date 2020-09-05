#pragma warning disable CA2227

using System;
using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a track.
    /// </summary>
    public class Track : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Track"/> class.
        /// </summary>
        /// <param name="album">The album.</param>
        public Track(MusicAlbum album)
        {
            if (album == null)
            {
                throw new ArgumentNullException(nameof(album));
            }

            album.Tracks.Add(this);

            Releases = new HashSet<Release>();
            TrackMetadata = new HashSet<TrackMetadata>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Track"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Track()
        {
        }

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        public int? TrackNumber { get; set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the track metadata.
        /// </summary>
        public virtual ICollection<TrackMetadata> TrackMetadata { get; protected set; }
    }
}
