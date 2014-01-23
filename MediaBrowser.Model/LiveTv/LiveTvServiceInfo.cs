using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ServiceInfo
    /// </summary>
    public class LiveTvServiceInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        public string HomePageUrl { get; set; }

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

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has update available.
        /// </summary>
        /// <value><c>true</c> if this instance has update available; otherwise, <c>false</c>.</value>
        public bool HasUpdateAvailable { get; set; }
    }

    public class GuideInfo
    {
        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime EndDate { get; set; }
    }

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

    public enum LiveTvServiceStatus
    {
        Ok = 0,
        Unavailable = 1
    }

    public enum LiveTvTunerStatus
    {
        Available = 0,
        Disabled = 1,
        RecordingTv = 2,
        LiveTv = 3
    }
}
