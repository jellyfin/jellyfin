#nullable disable
#pragma warning disable CS1591

using System;

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
        /// Gets or sets the product name. This is the AssemblyProduct name.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Gets or sets the operating system.
        /// </summary>
        /// <value>The operating system.</value>
        [Obsolete("This is no longer set")]
        public string OperatingSystem { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the startup wizard is completed.
        /// </summary>
        /// <remarks>
        /// Nullable for OpenAPI specification only to retain backwards compatibility in api clients.
        /// </remarks>
        /// <value>The startup completion status.</value>]
        public bool? StartupWizardCompleted { get; set; }
    }
}
