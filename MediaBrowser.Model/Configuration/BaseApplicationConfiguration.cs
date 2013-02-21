using ProtoBuf;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Serves as a common base class for the Server and UI application Configurations
    /// ProtoInclude tells Protobuf about subclasses,
    /// The number 50 can be any number, so long as it doesn't clash with any of the ProtoMember numbers either here or in subclasses.
    /// </summary>
    [ProtoContract, ProtoInclude(965, typeof(ServerConfiguration))]
    public class BaseApplicationConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable debug level logging].
        /// </summary>
        /// <value><c>true</c> if [enable debug level logging]; otherwise, <c>false</c>.</value>
        [ProtoMember(1)]
        public bool EnableDebugLevelLogging { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTP level logging].
        /// </summary>
        /// <value><c>true</c> if [enable HTTP level logging]; otherwise, <c>false</c>.</value>
        [ProtoMember(56)]
        public bool EnableHttpLevelLogging { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        [ProtoMember(2)]
        public int HttpServerPortNumber { get; set; }

        /// <summary>
        /// Enable automatically and silently updating of the application
        /// </summary>
        /// <value><c>true</c> if [enable auto update]; otherwise, <c>false</c>.</value>
        [ProtoMember(3)]
        public bool EnableAutoUpdate { get; set; }

        /// <summary>
        /// The number of days we should retain log files
        /// </summary>
        /// <value>The log file retention days.</value>
        [ProtoMember(5)]
        public int LogFileRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [run at startup].
        /// </summary>
        /// <value><c>true</c> if [run at startup]; otherwise, <c>false</c>.</value>
        [ProtoMember(58)]
        public bool RunAtStartup { get; set; }

        /// <summary>
        /// Gets or sets the legacy web socket port number.
        /// </summary>
        /// <value>The legacy web socket port number.</value>
        [ProtoMember(59)]
        public int LegacyWebSocketPortNumber { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationConfiguration" /> class.
        /// </summary>
        public BaseApplicationConfiguration()
        {
            HttpServerPortNumber = 8096;
            LegacyWebSocketPortNumber = 8945;

            EnableAutoUpdate = true;
            LogFileRetentionDays = 14;

            EnableHttpLevelLogging = true;

#if (DEBUG)
            EnableDebugLevelLogging = true;
#endif
        }
    }
}
