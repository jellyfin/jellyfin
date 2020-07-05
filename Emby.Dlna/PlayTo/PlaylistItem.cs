#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public PlaylistItem(StreamInfo streamInfo, DeviceProfile profile)
        {
            StreamInfo = streamInfo ?? throw new ArgumentNullException(nameof(streamInfo));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public string StreamUrl { get; set; } = string.Empty;

        public string Didl { get; set; } = string.Empty;

        public StreamInfo StreamInfo { get; }

        public DeviceProfile Profile { get; }
    }
}
