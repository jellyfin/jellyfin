#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class ArtistInfo : ItemLookupInfo
    {
        public List<SongInfo> SongInfos { get; set; }

        public ArtistInfo()
        {
            SongInfos = new List<SongInfo>();
        }
    }
}
