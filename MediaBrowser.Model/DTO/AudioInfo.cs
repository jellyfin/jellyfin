
namespace MediaBrowser.Model.DTO
{
    public class AudioInfo
    {
        public int BitRate { get; set; }
        public int Channels { get; set; }

        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
    }
}
