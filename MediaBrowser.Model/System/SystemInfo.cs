using MediaBrowser.Model.Updates;
using System.Collections.Generic;

namespace MediaBrowser.Model.System
{
    /// <summary>
    /// Class SystemInfo
    /// </summary>
    public class SystemInfo : PublicSystemInfo
    {
        /// <summary>
        /// Gets or sets the display name of the operating system.
        /// </summary>
        /// <value>The display name of the operating system.</value>
        public string OperatingSystemDisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running as service.
        /// </summary>
        /// <value><c>true</c> if this instance is running as service; otherwise, <c>false</c>.</value>
        public bool IsRunningAsService { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports running as service].
        /// </summary>
        /// <value><c>true</c> if [supports running as service]; otherwise, <c>false</c>.</value>
        public bool SupportsRunningAsService { get; set; }

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
        /// Gets or sets a value indicating whether [supports library monitor].
        /// </summary>
        /// <value><c>true</c> if [supports library monitor]; otherwise, <c>false</c>.</value>
        public bool SupportsLibraryMonitor { get; set; }

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
        /// Gets or sets the cache path.
        /// </summary>
        /// <value>The cache path.</value>
        public string CachePath { get; set; }

        /// <summary>
        /// Gets or sets the log path.
        /// </summary>
        /// <value>The log path.</value>
        public string LogPath { get; set; }

        /// <summary>
        /// Gets or sets the internal metadata path.
        /// </summary>
        /// <value>The internal metadata path.</value>
        public string InternalMetadataPath { get; set; }

        /// <summary>
        /// Gets or sets the transcoding temporary path.
        /// </summary>
        /// <value>The transcoding temporary path.</value>
        public string TranscodingTempPath { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        public int HttpServerPortNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTPS].
        /// </summary>
        /// <value><c>true</c> if [enable HTTPS]; otherwise, <c>false</c>.</value>
        public bool SupportsHttps { get; set; }

        /// <summary>
        /// Gets or sets the HTTPS server port number.
        /// </summary>
        /// <value>The HTTPS server port number.</value>
        public int HttpsPortNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has update available.
        /// </summary>
        /// <value><c>true</c> if this instance has update available; otherwise, <c>false</c>.</value>
        public bool HasUpdateAvailable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports automatic run at startup].
        /// </summary>
        /// <value><c>true</c> if [supports automatic run at startup]; otherwise, <c>false</c>.</value>
        public bool SupportsAutoRunAtStartup { get; set; }

        public string EncoderLocationType { get; set; }

        public Architecture SystemArchitecture { get; set; }

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
