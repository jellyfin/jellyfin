#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Rsddp;
using Emby.Dlna.Rssdp.Devices;
using Emby.Dlna.Rssdp.EventArgs;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Emby.Dlna.Rssdp
{
    /// <summary>
    /// Uses DeviceDiscovery to allow you to search the network for a particular device, device types, or UPnP service types.
    /// Listenings for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    public class SsdpPlayToLocator : SsdpInfrastructure, ISsdpPlayToLocator
    {
        private readonly List<DiscoveredSsdpDevice> _devices;
        private readonly SocketServer _socketServer;
        private readonly object _timerLock;
        private readonly ILogger<SsdpPlayToLocator> _logger;
        private readonly INetworkManager _networkManager;
        private readonly TimeSpan _defaultSearchWaitTime;
        private readonly TimeSpan _oneSecond;
        private Timer? _broadcastTimer;

        public SsdpPlayToLocator(SocketServer socketServer, ILogger<SsdpPlayToLocator> logger, INetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _socketServer = socketServer ?? throw new ArgumentNullException(nameof(socketServer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultSearchWaitTime = TimeSpan.FromSeconds(4);
            _oneSecond = TimeSpan.FromSeconds(1);
            _timerLock = new object();
            _devices = new List<DiscoveredSsdpDevice>();

            SsdpFilter = new string[]
            {
                "urn:schemas-upnp-org:device:MediaServer:",
                "urn:schemas-upnp-org:device:MediaRenderer:",
                "urn:schemas-upnp-org:device:MediaPlayer:",
                "urn:schemas-upnp-org:device:InternetGatewayDevice:"
            };

            _socketServer.ResponseReceived += ProcessSearchResponseMessage;
        }

        /// <summary>
        /// Raised for when
        /// <list type="bullet">
        /// <item>An 'alive' notification is received that a device, regardless of whether or not that device is not already in the cache or has
        /// previously raised this event.</item>
        /// <item>For each item found during a device (cached or not), allowing clients to respond to found devices
        /// before the entire search is complete.</item>
        /// </list>
        /// <para>This event may be raised from a background thread, if interacting with UI or other objects with specific thread affinity invoking
        /// to the relevant thread is required.</para>
        /// </summary>
        public event EventHandler<DeviceAvailableEventArgs>? DeviceAvailable;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        /// <remarks>
        /// <para>Devices *should* broadcast these types of notifications, but not all devices do and sometimes (in the event of power loss for example)
        /// it might not be possible for a device to do so. You should also implement error handling when trying to contact a device, even if RSSDP is
        /// reporting that device as available.</para>
        /// <para>The <see cref="DeviceUnavailableEventArgs.DiscoveredDevice"/> property may contain either a fully complete <see cref="DiscoveredSsdpDevice"/>
        /// instance, or one containing just a USN and NotificationType property. Full information is available if the device was previously discovered and cached,
        /// but only partial information if a byebye notification was received for a previously unseen or expired device.</para>
        /// <para>This event may be raised from a background thread, if interacting with UI or other objects with specific thread affinity invoking to the
        /// relevant thread is required.</para>
        /// </remarks>
        public event EventHandler<DeviceUnavailableEventArgs>? DeviceUnavailable;

        /// <summary>
        /// Gets or sets a string containing the filter for notifications. Notifications not matching the filter will not raise the
        /// <see cref="ISsdpPlayToLocator.DeviceAvailable"/> or <see cref="ISsdpPlayToLocator.DeviceUnavailable"/> events.
        /// </summary>
        /// <remarks>
        /// <para>Device alive/byebye notifications whose NT header does not match this filter value will still be captured and cached internally,
        /// but will not raise events about device availability. Usually used with either a device type of uuid NT header value.</para>
        /// <para>If the value is null or empty string then, all notifications are reported.</para>
        /// <para>Example filters follow.</para>
        /// <example>upnp:rootdevice</example>
        /// <example>urn:schemas-upnp-org:device:WANDevice:1</example>
        /// <example>uuid:9F15356CC-95FA-572E-0E99-85B456BD3012</example>
        /// </remarks>
        private string[] SsdpFilter { get; set; }

        public void Start(TimeSpan dueTime, TimeSpan period)
        {
            lock (_timerLock)
            {
                if (_broadcastTimer == null)
                {
                    _broadcastTimer = new Timer(OnBroadcastTimerCallback, null, dueTime, period);
                }
                else
                {
                    _broadcastTimer.Change(dueTime, period);
                }
            }
        }

        /// <summary>
        /// Disposes this object and all internal resources. Stops listening for all network messages.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed, or false is only unmanaged resources should be cleaned up.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_socketServer != null)
                {
                    _socketServer.ResponseReceived -= ProcessSearchResponseMessage;
                    _socketServer.RequestReceived -= ProcessNotificationMessage;
                }

                _logger.LogDebug("Disposing.");
                lock (_timerLock)
                {
                    _broadcastTimer?.Dispose();
                    _broadcastTimer = null;
                }
            }
        }

        private static TimeSpan CacheAgeFromHeader(CacheControlHeaderValue headerValue)
        {
            if (headerValue == null)
            {
                return TimeSpan.Zero;
            }

            return headerValue.MaxAge ?? headerValue.SharedMaxAge ?? TimeSpan.Zero;
        }

        private static DiscoveredSsdpDevice? FindExistingDeviceNotification(IEnumerable<DiscoveredSsdpDevice> devices, string notificationType, string usn)
        {
            foreach (var d in devices)
            {
                if (string.Equals(d.NotificationType, notificationType, StringComparison.Ordinal) &&
                    string.Equals(d.Usn, usn, StringComparison.Ordinal))
                {
                    return d;
                }
            }

            return null;
        }

        private static List<DiscoveredSsdpDevice> FindExistingDeviceNotifications(IList<DiscoveredSsdpDevice> devices, string usn)
        {
            var list = new List<DiscoveredSsdpDevice>();

            foreach (var d in devices)
            {
                if (string.Equals(d.Usn, usn, StringComparison.Ordinal))
                {
                    list.Add(d);
                }
            }

            return list;
        }

        private async void OnBroadcastTimerCallback(object state)
        {
            if (IsDisposed)
            {
                return;
            }

            _socketServer.RequestReceived += ProcessNotificationMessage;
            _socketServer.ResponseReceived += ProcessSearchResponseMessage;
            RemoveExpiredDevicesFromCache();

            try
            {
                await BroadcastDiscoverMessage(SearchTimeToMXValue(_defaultSearchWaitTime)).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchAsync failed.");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private bool AddOrUpdateDiscoveredDevice(DiscoveredSsdpDevice device, IPAddress localIpAddress)
        {
            bool isNewDevice = false;
            lock (_devices)
            {
                var existingDevice = FindExistingDeviceNotification(_devices, device.NotificationType, device.Usn);
                if (existingDevice == null)
                {
                    _devices.Add(device);
                    isNewDevice = true;
                }
                else
                {
                    _devices.Remove(existingDevice);
                    _devices.Add(device);
                }
            }

            DeviceAvailable?.Invoke(this, new DeviceAvailableEventArgs(device, isNewDevice, localIpAddress));

            return isNewDevice;
        }

        /// <summary>
        /// Returns true if the device type passed is one that we want.
        /// </summary>
        /// <param name="device">Device to check.</param>
        /// <returns>Result of the operation.</returns>
        private bool SsdpTypeMatchesFilter(DiscoveredSsdpDevice device)
        {
            return SsdpFilter.Where(m => device.NotificationType.StartsWith(m, StringComparison.OrdinalIgnoreCase)).Any();
        }

        private Task BroadcastDiscoverMessage(TimeSpan mxValue)
        {
            string[] multicastAddresses = { "239.255.255.250", "[ff02::C]", "[ff05::C]" };
            Task[] tasks = { Task.CompletedTask, Task.CompletedTask, Task.CompletedTask };
            int count = _networkManager.IsIP6Enabled ? 2 : 0;

            for (int a = 0; a <= count; a++)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["HOST"] = multicastAddresses[a] + ":1900",
                    ["USER-AGENT"] = _networkManager.SsdpUserAgent,
                    ["MAN"] = "\"ssdp:discover\"",
                    ["ST"] = "ssdp:all",
                    ["MX"] = mxValue.Seconds.ToString(CultureInfo.CurrentCulture)
                };

                var message = BuildMessage("M-SEARCH * HTTP/1.1", values);

                tasks[a] = _socketServer.SendMulticastMessageAsync(message, a == 0 ? IPAddress.Any : IPAddress.IPv6Any);
            }

            _logger.LogDebug("-> M-SEARCH");
            return Task.WhenAll(tasks);
        }

        private void ProcessSearchResponseMessage(object sender, ResponseReceivedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            HttpResponseMessage message = e.Message;

            if (!message.IsSuccessStatusCode)
            {
                return;
            }

            var location = GetFirstHeaderUriValue("Location", message.Headers);
            if (location != null)
            {
                var device = new DiscoveredSsdpDevice(
                    DateTimeOffset.Now,
                    CacheAgeFromHeader(message.Headers.CacheControl),
                    location,
                    GetFirstHeaderValue("ST", message.Headers),
                    GetFirstHeaderValue("USN", message.Headers),
                    message.Headers);

                if (!SsdpTypeMatchesFilter(device))
                {
                    // Filtered type - not interested.
                    return;
                }

                if (AddOrUpdateDiscoveredDevice(device, e.LocalIPAddress))
                {
                    _logger.LogDebug("Found DLNA Device : {0} {1}", device.DescriptionLocation, e.LocalIPAddress);
                }
            }
        }

        private void ProcessNotificationMessage(object sender, RequestReceivedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            HttpRequestMessage message = e.Message;
            IPAddress localIpAddress = e.LocalIPAddress;

            var notificationType = GetFirstHeaderValue("NTS", message.Headers);

            var device = new DiscoveredSsdpDevice(
                   DateTimeOffset.Now,
                   CacheAgeFromHeader(message.Headers.CacheControl),
                   GetFirstHeaderUriValue("Location", message.Headers),
                   GetFirstHeaderValue("NT", message.Headers),
                   GetFirstHeaderValue("USN", message.Headers),
                   message.Headers);

            if (!SsdpTypeMatchesFilter(device))
            {
                // Filtered type - not interested.
                return;
            }

            if (device.DescriptionLocation != null)
            {
                if (notificationType.StartsWith("urn:schemas-upnp-org:device:InternetGatewayDevice:", StringComparison.OrdinalIgnoreCase))
                {
                    // If uPNP is running and the message didn't originate from mono - pass these messages to mono.nat. It might want them.
                    if (_networkManager.IsuPnPActive && !e.Simulated)
                    {
                        // _logger.LogDebug("Passing NOTIFY message to Mono.Nat.");
                        NatUtility.ParseMessage(NatProtocol.Upnp, localIpAddress, e.Raw, e.ReceivedFrom);
                        return;
                    }

                    return;
                }

                if (string.Equals(notificationType, "ssdp:alive", StringComparison.OrdinalIgnoreCase))
                {
                    // Process Alive Notification.
                    if (AddOrUpdateDiscoveredDevice(device, localIpAddress))
                    {
                        _logger.LogDebug("<- ALIVE : {0} ", device.DescriptionLocation);
                    }

                    return;
                }
            }

            if (!string.IsNullOrEmpty(device.NotificationType) && string.Equals(notificationType, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                // Process ByeBye Notification.
                if (!DeviceDied(device.Usn, false))
                {
                    _logger.LogDebug("Byebye: {0}", device);

                    DeviceUnavailable?.Invoke(this, new DeviceUnavailableEventArgs(device, false));
                }
            }
        }

        private void RemoveExpiredDevicesFromCache()
        {
            if (IsDisposed)
            {
                return;
            }

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly: Syntax checker cannot cope with a null array x[]?
            DiscoveredSsdpDevice[]? expiredDevices = null;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
            lock (_devices)
            {
                expiredDevices = (from device in _devices where device.IsExpired() select device).ToArray();

                foreach (var device in expiredDevices)
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    _devices.Remove(device);
                }
            }

            // Don't do this inside lock because DeviceDied raises an event which means public code may execute during lock and cause problems.
            foreach (var expiredUsn in (from expiredDevice in expiredDevices select expiredDevice.Usn).Distinct())
            {
                if (IsDisposed)
                {
                    return;
                }

                DeviceDied(expiredUsn, true);
            }
        }

        private bool DeviceDied(string deviceUsn, bool expired)
        {
            List<DiscoveredSsdpDevice>? existingDevices = null;
            lock (_devices)
            {
                existingDevices = FindExistingDeviceNotifications(_devices, deviceUsn);
                foreach (var existingDevice in existingDevices)
                {
                    if (IsDisposed)
                    {
                        return true;
                    }

                    _devices.Remove(existingDevice);
                }
            }

            if (existingDevices != null && existingDevices.Count > 0)
            {
                foreach (var removedDevice in existingDevices)
                {
                    DeviceUnavailable?.Invoke(this, new DeviceUnavailableEventArgs(removedDevice, expired));
                }

                return true;
            }

            return false;
        }

        private TimeSpan SearchTimeToMXValue(TimeSpan searchWaitTime)
        {
            if (searchWaitTime.TotalSeconds < 2 || searchWaitTime == TimeSpan.Zero)
            {
                return _oneSecond;
            }
            else
            {
                return searchWaitTime.Subtract(_oneSecond);
            }
        }
    }
}
