#nullable enable

namespace Emby.Dlna.Configuration
{
    /// <summary>
    /// The DlnaOptions class contains the user definable parameters for the dlna subsystems.
    /// </summary>
    public class DlnaOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaOptions"/> class.
        /// </summary>
        public DlnaOptions()
        {
            EnablePlayTo = true;
            EnableServer = true;
            ClientDiscoveryIntervalSeconds = 60;
            BlastAliveMessageIntervalSeconds = 1800;
        }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets a value to indicate the status of the dlna playTo subsystem.
        /// </summary>
        public bool EnablePlayTo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets a value to indicate the status of the dlna server subsystem.
        /// </summary>
        public bool EnableServer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets a value to indicate the whether details ssdp debug logs should be sent to the console/log.
        /// If the setting "Emby.Dlna": "Debug" is included in logging.default.json, a trace of all ssdp packets sent and received,
        /// will also be sent to the console/log.
        /// </summary>
        public bool EnableDebugLog { get; set; }

        /// <summary>
        /// Gets or sets the ssdp client discovery interval time (in seconds).
        /// This is the time after which the server will send a ssdp search request.
        /// </summary>
        public int ClientDiscoveryIntervalSeconds { get; set; }

        // TODO: Rename this to AliveMessageIntervalSeconds. It better describes what this function does.

        /// <summary>
        /// Gets or sets the frequency at which ssdp alive notifications are transmitted.
        /// </summary>
        public int BlastAliveMessageIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the default user account that the dlna server uses.
        /// </summary>
        public string? DefaultUserId { get; set; }

        /// <summary>
        /// Gets or sets the number of times SSDP UDP messages are sent.
        /// </summary>
        public int UDPSendCount { get; set; } = 2;
    }
}
