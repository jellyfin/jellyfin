using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class AlbumInfo : ItemLookupInfo
    {
        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public IReadOnlyList<string> AlbumArtists { get; set; }

        /// <summary>
        /// Gets or sets the artist provider ids.
        /// </summary>
        /// <value>The artist provider ids.</value>
        public Dictionary<string, string> ArtistProviderIds { get; set; }

        public List<SongInfo> SongInfos { get; set; }

        public AlbumInfo()
        {
            ArtistProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SongInfos = new List<SongInfo>();
            AlbumArtists = Array.Empty<string>();
        }
    }
}
