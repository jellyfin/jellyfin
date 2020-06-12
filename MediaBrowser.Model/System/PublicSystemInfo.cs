#nullable disable
#pragma warning disable CS1591

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
        /// Gets or sets the name of the server.
        /// </summary>
        /// <value>The name of the server.</value>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the server version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// The product name. This is the AssemblyProduct name.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Gets or sets the operating system.
        /// </summary>
        /// <value>The operating system.</value>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }
}
