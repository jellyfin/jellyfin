#pragma warning disable CA1002, CA2227, CS1591

using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LookupDtos
{
    public class ArtistInfo : ItemLookupInfo
    {
        public ArtistInfo()
        {
#pragma warning disable CS0618 // The deprecated members are still carried until they are removed.
            SongInfos = new List<SongInfo>();
#pragma warning restore CS0618
        }

        /// <summary>
        /// Gets or sets the songs of the artist. Deprecated, no search provider reads it.
        /// </summary>
        [Obsolete("Server side only. Filled in from the artist's tracks when the server looks the artist up.")]
        public List<SongInfo> SongInfos { get; set; }
    }
}
