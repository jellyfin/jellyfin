using System;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class InstallationInfo.
    /// </summary>
    public class InstallationInfo
    {
        /// <summary>
        /// Gets or sets the guid.
        /// </summary>
        /// <value>The guid.</value>
        public string Guid { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }
    }
}
