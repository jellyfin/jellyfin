#pragma warning disable CA1002, CA2227, CS1591

using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LookupDtos
{
    public class AlbumInfo : ItemLookupInfo
    {
        public AlbumInfo()
        {
#pragma warning disable CS0618 // The deprecated members are still carried until they are removed.
            ArtistProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SongInfos = new List<SongInfo>();
#pragma warning restore CS0618
            AlbumArtists = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public IReadOnlyList<string> AlbumArtists { get; set; }

        /// <summary>
        /// Gets or sets the artist provider ids. Deprecated, use ProviderIds instead.
        /// </summary>
        /// <value>The artist provider ids.</value>
        [Obsolete("Server side only. Filled in from the parent artist when the server looks the album up.")]
        public Dictionary<string, string> ArtistProviderIds { get; set; }

        /// <summary>
        /// Gets or sets the songs of the album. Deprecated, use AlbumArtists instead.
        /// </summary>
        [Obsolete("Server side only. Filled in from the album's tracks when the server looks the album up.")]
        public List<SongInfo> SongInfos { get; set; }
    }
}
