#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Common.Networking
{
    /// <summary>
    /// Wake-On-LAN Class.
    /// For use when the storage is separate from the webserver.
    /// </summary>
    public class WakeOnLAN
    {
        /// <summary>
        /// Singleton instance of this object.
        /// </summary>
#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable SA1401 // Fields should be private
        public static WakeOnLAN? Instance;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore CA2211 // Non-constant fields should not be visible

        /// <summary>
        /// Threading object for network interfaces.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// ILogger instance.
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Last time a request was received.
        /// </summary>
        private DateTime _lastRequestReceived;

        /// <summary>
        /// Timeout to use between WOL.
        /// </summary>
        private int _timeout;

        /// <summary>
        /// Gets the MACWakeupList user setting from config.
        /// </summary>
        private Func<string[]> _wolmacFn = null!;

        /// <summary>
        /// Gets the MACWakeupTimeout user setting from config.
        /// </summary>
        private Func<int> _timeoutFn = null!;

        /// <summary>
        /// Holds the list of MAC addresses for use in WOL operations.
        /// </summary>
        private string[] _wolMACList = null!;

        /// <summary>
        /// Gets the MACMulicastPort user setting from config.
        /// </summary>
        private Func<int> _portFn = null!;

        /// <summary>
        /// Port to use.
        /// </summary>
        private int _port = 9;

        private INetworkManager _networkManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WakeOnLAN"/> class.
        /// </summary>
        /// <param name="logger">ILogger to use.</param>
        /// <param name="settings">Function to retrieve the MACWakeupList user setting from config.</param>
        /// <param name="timeoutFn">Function to retrieve the MACWakeupTimeout user setting from config.</param>
        /// <param name="portFn">Function to MACMulicastPort user setting from config.</param>
        /// <param name="networkManager">NetworkManager object.</param>
        public WakeOnLAN(ILogger logger, Func<string[]> settings, Func<int> timeoutFn, Func<int> portFn, INetworkManager networkManager)
        {
            _logger = logger;
            _lastRequestReceived = DateTime.Now.AddDays(-1); // Set to Yesterday, so WOL will work the first time around.

            _wolmacFn = settings ?? throw new ArgumentNullException(nameof(settings));
            _wolMACList = settings() ?? Array.Empty<string>();
            _timeoutFn = timeoutFn;
            _timeout = _timeoutFn();
            _portFn = portFn;
            _port = _portFn();
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            WakeOnLAN.Instance = this;
        }

        /// <summary>
        /// Reloads the setttings.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event parameters.</param>
        public void ConfigurationUpdated(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _wolMACList = _wolmacFn() ?? Array.Empty<string>();
                _timeout = _timeoutFn();
                _port = _portFn();
            }
        }

        /// <summary>
        /// Triggers an event that may need a WOL magic packet sending.
        /// WOL events are only triggered at a max of _timeout minutes.
        /// </summary>
        public void WakeUpResources()
        {
            TimeSpan diff = DateTime.Now - _lastRequestReceived;
            if (diff.TotalMinutes > _timeout)
            {
                _lastRequestReceived = DateTime.Now;
                lock (_lock)
                {
                    foreach (var mac in _wolMACList)
                    {
                        try
                        {
                            WakeOnLan(mac);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "WOL error to {0}.", mac);
                        }
                    }
                }
            }
        }

        // Code adapted from https://stackoverflow.com/questions/861873/wake-on-lan-using-c-sharp

        /// <summary>
        /// Builds a WOL magic packet.
        /// </summary>
        /// <param name="macAddress">MAC address to send it to. MacAddress in any standard HEX format.</param>
        /// <returns>Byte array containg the magic packet.</returns>
        private static byte[] BuildMagicPacket(string macAddress)
        {
            macAddress = Regex.Replace(macAddress, "[: -]", string.Empty);
            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(macAddress.Substring(i * 2, 2), 16);
            }

            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // First 6 times 0xff.
                for (int i = 0; i < 6; i++)
                {
                    bw.Write((byte)0xff);
                }

                // Then 16 times MacAddress.
                for (int i = 0; i < 16; i++)
                {
                    bw.Write(macBytes);
                }
            }

            return ms.ToArray(); // 102 bytes magic packet.
        }

        /// <summary>
        /// Send a WOL magic packet across the LAN interfaces.
        /// </summary>
        /// <param name="macAddress">Destination MAC.</param>
        private async void WakeOnLan(string macAddress)
        {
            byte[] magicPacket = BuildMagicPacket(macAddress);

            foreach (IPNetAddress addr in _networkManager.GetInternalInterfaceAddresses())
            {
                if (!addr.IsLoopback())
                {
                    _logger.LogDebug("Multicasting to {0} on interface {1}.", macAddress, addr.Address);
                    if (addr.IsIP6())
                    {
                        await SendWakeOnLan(addr.Address, IPAddress.Parse("ff02::1"), magicPacket).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendWakeOnLan(addr.Address, IPAddress.Parse("224.0.0.1"), magicPacket).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a WOL a magic packet out as a multicast .
        /// </summary>
        /// <param name="localIpAddress">Interface to use.</param>
        /// <param name="multicastIpAddress">Multicast address.</param>
        /// <param name="magicPacket">Magic packet to send.</param>
        /// <returns>Task id.</returns>
        private async Task SendWakeOnLan(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket)
        {
            try
            {
#pragma warning disable IDE0063 // Use simple 'using' statement
                using (UdpClient client = new UdpClient(new IPEndPoint(localIpAddress, _port)))
                {
                    await client.SendAsync(magicPacket, magicPacket.Length, multicastIpAddress.ToString(), _port).ConfigureAwait(false);
                }
#pragma warning restore IDE0063 // Use simple 'using' statement
            }
            catch
            {
                _logger.LogError("error sending WOL to {0}.", localIpAddress);
            }
        }
    }
}
