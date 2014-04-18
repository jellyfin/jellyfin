using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public int PlayState { get; set; }

        public string StreamUrl { get; set; }

        public string Didl { get; set; }

        public StreamInfo StreamInfo { get; set; }
    }
}