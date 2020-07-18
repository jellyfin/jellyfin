#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvInfo
    {
        public LiveTvInfo()
        {
            Services = Array.Empty<LiveTvServiceInfo>();
            EnabledUsers = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        /// <value>The services.</value>
        public LiveTvServiceInfo[] Services { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the enabled users.
        /// </summary>
        /// <value>The enabled users.</value>
        public string[] EnabledUsers { get; set; }
    }
}
