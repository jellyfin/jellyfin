#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public string StreamUrl { get; set; }

        public string Didl { get; set; }

        public StreamInfo StreamInfo { get; set; }

        public DeviceProfile Profile { get; set; }
    }
}
