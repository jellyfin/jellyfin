using MediaBrowser.Model.Updates;
using ProtoBuf;

namespace MediaBrowser.Model.System
{
    /// <summary>
    /// Class SystemInfo
    /// </summary>
    [ProtoContract]
    public class SystemInfo
    {
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [ProtoMember(1)]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has pending restart.
        /// </summary>
        /// <value><c>true</c> if this instance has pending restart; otherwise, <c>false</c>.</value>
        [ProtoMember(2)]
        public bool HasPendingRestart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is network deployed.
        /// </summary>
        /// <value><c>true</c> if this instance is network deployed; otherwise, <c>false</c>.</value>
        [ProtoMember(3)]
        public bool IsNetworkDeployed { get; set; }

        /// <summary>
        /// Gets or sets the in progress installations.
        /// </summary>
        /// <value>The in progress installations.</value>
        [ProtoMember(4)]
        public InstallationInfo[] InProgressInstallations { get; set; }

        /// <summary>
        /// Gets or sets the web socket port number.
        /// </summary>
        /// <value>The web socket port number.</value>
        [ProtoMember(5)]
        public int WebSocketPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the completed installations.
        /// </summary>
        /// <value>The completed installations.</value>
        [ProtoMember(6)]
        public InstallationInfo[] CompletedInstallations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [supports native web socket].
        /// </summary>
        /// <value><c>true</c> if [supports native web socket]; otherwise, <c>false</c>.</value>
        [ProtoMember(7)]
        public bool SupportsNativeWebSocket { get; set; }

        /// <summary>
        /// Gets or sets plugin assemblies that failed to load.
        /// </summary>
        /// <value>The failed assembly loads.</value>
        public string[] FailedPluginAssemblies { get; set; }
    }
}
