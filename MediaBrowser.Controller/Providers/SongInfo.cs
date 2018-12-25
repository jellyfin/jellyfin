
namespace MediaBrowser.Controller.Providers
{
    public class SongInfo : ItemLookupInfo
    {
        public string[] AlbumArtists { get; set; }
        public string Album { get; set; }
        public string[] Artists { get; set; }

        public SongInfo()
        {
            Artists = EmptyStringArray;
            AlbumArtists = EmptyStringArray;
        }
    }
}