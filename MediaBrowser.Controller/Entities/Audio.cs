
namespace MediaBrowser.Controller.Entities
{
    public class Audio : BaseItem
    {
        public int BitRate { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }

        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
    }
}
