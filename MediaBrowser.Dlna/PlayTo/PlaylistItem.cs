using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public string ItemId { get; set; }

        public string MediaSourceId { get; set; }
        
        public bool Transcode { get; set; }

        public DlnaProfileType MediaType { get; set; }

        public string Container { get; set; }

        public string MimeType { get; set; }

        public int PlayState { get; set; }

        public string StreamUrl { get; set; }

        public string DlnaHeaders { get; set; }

        public string Didl { get; set; }

        public long StartPositionTicks { get; set; }
    }
}