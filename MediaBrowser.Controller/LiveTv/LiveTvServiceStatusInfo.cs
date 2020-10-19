#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvServiceStatusInfo
    {
        public LiveTvServiceStatusInfo()
        {
            Tuners = new List<LiveTvTunerInfo>();
            IsVisible = true;
        }

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

        /// <summary>
        /// Gets or sets the tuners.
        /// </summary>
        /// <value>The tuners.</value>
        public List<LiveTvTunerInfo> Tuners { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value><c>true</c> if this instance is visible; otherwise, <c>false</c>.</value>
        public bool IsVisible { get; set; }
    }
}
