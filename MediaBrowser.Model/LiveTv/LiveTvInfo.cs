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
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the enabled users.
        /// </summary>
        /// <value>The enabled users.</value>
        public List<string> EnabledUsers { get; set; }

        public LiveTvInfo()
        {
            Services = new List<LiveTvServiceInfo>();
            EnabledUsers = new List<string>();
        }
    }
}