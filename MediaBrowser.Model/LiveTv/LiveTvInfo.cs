using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvInfo
    {
        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        /// <value>The services.</value>
        public List<LiveTvServiceInfo> Services { get; set; }

        /// <summary>
        /// Gets or sets the name of the active service.
        /// </summary>
        /// <value>The name of the active service.</value>
        public string ActiveServiceName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the enabled users.
        /// </summary>
        /// <value>The enabled users.</value>
        public List<string> EnabledUsers { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public LiveTvServiceStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        /// <value>The status message.</value>
        public string StatusMessage { get; set; }

        public LiveTvInfo()
        {
            Services = new List<LiveTvServiceInfo>();
            EnabledUsers = new List<string>();
        }
    }
}