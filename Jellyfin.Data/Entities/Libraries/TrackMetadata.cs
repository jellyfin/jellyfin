using System;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity holding metadata for a track.
    /// </summary>
    public class TrackMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        /// <param name="track">The track.</param>
        public TrackMetadata(string title, string language, Track track) : base(title, language)
        {
            if (track == null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            track.TrackMetadata.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMetadata"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected TrackMetadata()
        {
        }
    }
}
