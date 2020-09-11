using System.Net.NetworkInformation;

namespace MediaBrowser.Model.System
{
    /// <summary>
    /// Provides the MAC address and port for wake-on-LAN functionality.
    /// </summary>
    public class WakeOnLanInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WakeOnLanInfo" /> class.
        /// </summary>
        /// <param name="macAddress">The MAC address.</param>
        public WakeOnLanInfo(PhysicalAddress macAddress) : this(macAddress.ToString())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WakeOnLanInfo" /> class.
        /// </summary>
        /// <param name="macAddress">The MAC address.</param>
        public WakeOnLanInfo(string macAddress) : this()
        {
            MacAddress = macAddress;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WakeOnLanInfo" /> class.
        /// </summary>
        public WakeOnLanInfo()
        {
            Port = 9;
        }

        /// <summary>
        /// Gets the MAC address of the device.
        /// </summary>
        /// <value>The MAC address.</value>
        public string? MacAddress { get; set; }

        /// <summary>
        /// Gets or sets the wake-on-LAN port.
        /// </summary>
        /// <value>The wake-on-LAN port.</value>
        public int Port { get; set; }
    }
}
