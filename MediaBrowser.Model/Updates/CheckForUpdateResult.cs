using System;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class CheckForUpdateResult
    /// </summary>
    public class CheckForUpdateResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is update available.
        /// </summary>
        /// <value><c>true</c> if this instance is update available; otherwise, <c>false</c>.</value>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        /// Gets or sets the available version.
        /// </summary>
        /// <value>The available version.</value>
        public Version AvailableVersion { get; set; }
    }
}
