using MediaBrowser.Model.Dlna;

namespace Emby.Server.Implementations.Sync
{
    public class SyncJobOptions
    {
        /// <summary>
        /// Gets or sets the conversion options.
        /// </summary>
        /// <value>The conversion options.</value>
        public DeviceProfile DeviceProfile { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is converting.
        /// </summary>
        /// <value><c>true</c> if this instance is converting; otherwise, <c>false</c>.</value>
        public bool IsConverting { get; set; }
    }
}
