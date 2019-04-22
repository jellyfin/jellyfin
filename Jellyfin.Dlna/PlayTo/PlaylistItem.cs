using Jellyfin.Model.Dlna;

namespace Jellyfin.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public string StreamUrl { get; set; }

        public string Didl { get; set; }

        public StreamInfo StreamInfo { get; set; }

        public DeviceProfile Profile { get; set; }
    }
}
