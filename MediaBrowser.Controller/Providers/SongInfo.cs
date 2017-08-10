using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class SongInfo : ItemLookupInfo
    {
        public string[] AlbumArtists { get; set; }
        public string Album { get; set; }
        public List<string> Artists { get; set; }

        public SongInfo()
        {
            Artists = new List<string>();
            AlbumArtists = EmptyStringArray;
        }
    }
}