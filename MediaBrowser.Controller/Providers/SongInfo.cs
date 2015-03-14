using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class SongInfo : ItemLookupInfo
    {
        public List<string> AlbumArtists { get; set; }
        public string Album { get; set; }
        public List<string> Artists { get; set; }

        public SongInfo()
        {
            Artists = new List<string>();
            AlbumArtists = new List<string>();
        }
    }
}