#nullable disable

namespace Jellyfin.LiveTv.TunerHosts.HdHomerun
{
    internal class Channels
    {
        public string GuideNumber { get; set; }

        public string GuideName { get; set; }

        public string VideoCodec { get; set; }

        public string AudioCodec { get; set; }

        public string URL { get; set; }

        public bool Favorite { get; set; }

        public bool DRM { get; set; }

        public bool HD { get; set; }
    }
}
