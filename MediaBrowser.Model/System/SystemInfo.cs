using MediaBrowser.Model.Updates;
using System.Collections.Generic;

namespace MediaBrowser.Model.System
{
    /// <summary>
    /// Class SystemInfo
    /// </summary>
    public class SystemInfo
    {
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the operating sytem.
        /// </summary>
        /// <value>The operating sytem.</value>
        public string OperatingSystem { get; set; }
        
        /// <summary>
        /// Gets or sets the mac address.
        /// </summary>
        /// <value>The mac address.</value>
        public string MacAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has pending restart.
        /// </summary>
        /// <value><c>true</c> if this instance has pending restart; otherwise, <c>false</c>.</value>
        public bool HasPendingRestart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is network deployed.
        /// </summary>
        /// <value><c>true</c> if this instance is network deployed; otherwise, <c>false</c>.</value>
        public bool IsNetworkDeployed { get; set; }

        /// <summary>
        /// Gets or sets the in progress installations.
        /// </summary>
        /// <value>The in progress installations.</value>
        public List<InstallationInfo> InProgressInstallations { get; set; }

        /// <summary>
        /// Gets or sets the web socket port number.
        /// </summary>
        /// <value>The web socket port number.</value>
        public int WebSocketPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the completed installations.
        /// </summary>
        /// <value>The completed installations.</value>
        public List<InstallationInfo> CompletedInstallations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports native web socket].
        /// </summary>
        /// <value><c>true</c> if [supports native web socket]; otherwise, <c>false</c>.</value>
        public bool SupportsNativeWebSocket { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public bool CanSelfRestart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate { get; set; }

        /// <summary>
        /// Gets or sets plugin assemblies that failed to load.
        /// </summary>
        /// <value>The failed assembly loads.</value>
        public List<string> FailedPluginAssemblies { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the program data path.
        /// </summary>
        /// <value>The program data path.</value>
        public string ProgramDataPath { get; set; }

        /// <summary>
        /// Gets or sets the items by name path.
        /// </summary>
        /// <value>The items by name path.</value>
        public string ItemsByNamePath { get; set; }

        /// <summary>
        /// Gets or sets the log path.
        /// </summary>
        /// <value>The log path.</value>
        public string LogPath { get; set; }
        
        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        public int HttpServerPortNumber { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInfo" /> class.
        /// </summary>
        public SystemInfo()
        {
            InProgressInstallations = new List<InstallationInfo>();

            CompletedInstallations = new List<InstallationInfo>();

            FailedPluginAssemblies = new List<string>();
        }
    }
}
