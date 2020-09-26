using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Defines the <see cref="PlaylistItem" />.
    /// </summary>
    public class PlaylistItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistItem"/> class.
        /// </summary>
        /// <param name="streamInfo">The <see cref="StreamInfo"/>.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        public PlaylistItem(StreamInfo streamInfo, DeviceProfile profile)
        {
            StreamInfo = streamInfo;
            Profile = profile;
            StreamUrl = string.Empty;
            Didl = string.Empty;
        }

        /// <summary>
        /// Gets or sets the stream's Url.
        /// </summary>
        public string StreamUrl { get; set; }

        /// <summary>
        /// Gets or sets the Didl xml.
        /// </summary>
        public string Didl { get; set; }

        /// <summary>
        /// Gets the stream information.
        /// </summary>
        public StreamInfo StreamInfo { get; }

        /// <summary>
        /// Gets the device profile.
        /// </summary>
        public DeviceProfile Profile { get; }
    }
}
