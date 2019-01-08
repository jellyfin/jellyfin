using Jellyfin.Versioning;

namespace MediaBrowser.Model.System
{
    public class PublicSystemInfo
    {
        /// <summary>
        /// Gets or sets the local address.
        /// </summary>
        /// <value>The local address.</value>
        public string LocalAddress { get; set; }

        /// <summary>
        /// Gets or sets the wan address.
        /// </summary>
        /// <value>The wan address.</value>
        public string WanAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the server.
        /// </summary>
        /// <value>The name of the server.</value>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the API version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the server version.
        /// </summary>
        /// <value>The server version.</value>
        public string ServerVersion { get; set; }

        /// <summary>
        /// Gets or sets the build version.
        /// </summary>
        /// <value>The build version.</value>
        public ExtendedVersion ExtendedVersion { get; set; }

        /// <summary>
        /// Gets or sets the operating sytem.
        /// </summary>
        /// <value>The operating sytem.</value>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }
}
