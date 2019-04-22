using System.Collections.Generic;

namespace Jellyfin.Controller.Providers
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
