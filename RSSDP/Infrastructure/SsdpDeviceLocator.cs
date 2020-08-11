#pragma warning disable CA1303 // Do not pass literals as localized parameters

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using Rssdp.Events;
using Rssdp.Devices;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Uses DeviceDiscovery to allow you to search the network for a particular device, device types, or UPnP service types.
    /// Listenings for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    public class SsdpDeviceLocator : ISsdpInfrastructure, ISsdpDeviceLocator
    {
        private const string SsdpUserAgent = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";

        private readonly List<DiscoveredSsdpDevice> _devices;
        private readonly SocketServer _socketServer;
        private readonly object _timerLock;
        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;
        private readonly TimeSpan _defaultSearchWaitTime;
        private readonly TimeSpan _oneSecond;
        private readonly string _systemId;
        private Timer? _broadcastTimer;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SsdpDeviceLocator(SocketServer socketServer, ILogger logger, INetworkManager networkManager, string systemId)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _socketServer = socketServer ?? throw new ArgumentNullException(nameof(socketServer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultSearchWaitTime = TimeSpan.FromSeconds(4);
            _oneSecond = TimeSpan.FromSeconds(1);
            _systemId = systemId;
            _timerLock = new object();
            _devices = new List<DiscoveredSsdpDevice>();
            
            _socketServer.ResponseReceived += ProcessSearchResponseMessage;
        }

        /// <summary>
        /// Raised for when
        /// <list type="bullet">
        /// <item>An 'alive' notification is received that a device, regardless of whether or not that device is not already in the cache or has
        /// previously raised this event.</item>
        /// <item>For each item found during a device <see cref="SearchAsync()"/> (cached or not), allowing clients to respond to found devices
        /// before the entire search is complete.</item>
        /// <item>Only if the notification type matches the <see cref="NotificationFilter"/> property. By default the filter is null, meaning all
        /// notifications raise events (regardless of ant </item>
        /// </list>
        /// <para>This event may be raised from a background thread, if interacting with UI or other objects with specific thread affinity invoking
        /// to the relevant thread is required.</para>
        /// </summary>
        /// <seealso cref="NotificationFilter"/>
        /// <seealso cref="DeviceUnavailable"/>
        /// <seealso cref="StartListeningForNotifications"/>
        /// <seealso cref="StopListeningForNotifications"/>
        public event EventHandler<DeviceAvailableEventArgs>? DeviceAvailable;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        /// <remarks>
        /// <para>Devices *should* broadcast these types of notifications, but not all devices do and sometimes (in the event of power loss for example)
        /// it might not be possible for a device to do so. You should also implement error handling when trying to contact a device, even if RSSDP is
        /// reporting that device as available.</para>
        /// <para>This event is only raised if the notification type matches the <see cref="NotificationFilter"/> property. A null or empty string for
        /// the <see cref="NotificationFilter"/> will be treated as no filter and raise the event for all notifications.</para>
        /// <para>The <see cref="DeviceUnavailableEventArgs.DiscoveredDevice"/> property may contain either a fully complete <see cref="DiscoveredSsdpDevice"/>
        /// instance, or one containing just a USN and NotificationType property. Full information is available if the device was previously discovered and cached,
        /// but only partial information if a byebye notification was received for a previously unseen or expired device.</para>
        /// <para>This event may be raised from a background thread, if interacting with UI or other objects with specific thread affinity invoking to the
        /// relevant thread is required.</para>
        /// </remarks>
        /// <seealso cref="NotificationFilter"/>
        /// <seealso cref="DeviceAvailable"/>
        /// <seealso cref="StartListeningForNotifications"/>
        /// <seealso cref="StopListeningForNotifications"/>
        public event EventHandler<DeviceUnavailableEventArgs>? DeviceUnavailable;


        /// <summary>
        /// Sets or returns a string containing the filter for notifications. Notifications not matching the filter will not raise the
        /// <see cref="ISsdpDeviceLocator.DeviceAvailable"/> or <see cref="ISsdpDeviceLocator.DeviceUnavailable"/> events.
        /// </summary>
        /// <remarks>
        /// <para>Device alive/byebye notifications whose NT header does not match this filter value will still be captured and cached internally,
        /// but will not raise events about device availability. Usually used with either a device type of uuid NT header value.</para>
        /// <para>If the value is null or empty string then, all notifications are reported.</para>
        /// <para>Example filters follow;</para>
        /// <example>upnp:rootdevice</example>
        /// <example>urn:schemas-upnp-org:device:WANDevice:1</example>
        /// <example>uuid:9F15356CC-95FA-572E-0E99-85B456BD3012</example>
        /// </remarks>
        /// <seealso cref="ISsdpDeviceLocator.DeviceAvailable"/>
        /// <seealso cref="ISsdpDeviceLocator.DeviceUnavailable"/>
        /// <seealso cref="ISsdpDeviceLocator.StartListeningForNotifications"/>
        /// <seealso cref="ISsdpDeviceLocator.StopListeningForNotifications"/>
        public string? NotificationFilter { get; set; }

        public void RestartBroadcastTimer(TimeSpan dueTime, TimeSpan period)
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

        private async void OnBroadcastTimerCallback(object state)
        {
            StartListeningForNotifications();
            RemoveExpiredDevicesFromCache();

            try
            {
                await SearchAsync(_defaultSearchWaitTime).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "SearchAsync failed.");
            }
        }

        /// <summary>
        /// Performs a search for all devices using the default search timeout.
        /// </summary>
        public Task SearchAsync()
        {
            return SearchAsync(_defaultSearchWaitTime);
        }

        public Task SearchAsync(TimeSpan searchWaitTime)
        { 
            if (searchWaitTime.TotalSeconds < 0)
            {
                throw new ArgumentException("searchWaitTime must be a positive time.");
            }

            if (searchWaitTime.TotalSeconds > 0 && searchWaitTime.TotalSeconds <= 1)
            {
                throw new ArgumentException("searchWaitTime must be zero (if you are not using the result and relying entirely in the events), or greater than one second.");
            }

            ThrowIfDisposed();

            return BroadcastDiscoverMessage(SearchTimeToMXValue(searchWaitTime));
        }

        /// <summary>
        /// Starts listening for broadcast notifications of service availability.
        /// </summary>
        /// <remarks>
        /// <para>When called the system will listen for 'alive' and 'byebye' notifications. This can speed up searching,
        /// as well as provide dynamic notification of new devices appearing on the network, and previously discovered devices disappearing.</para>
        /// </remarks>
        /// <seealso cref="StopListeningForNotifications"/>
        /// <seealso cref="DeviceAvailable"/>
        /// <seealso cref="DeviceUnavailable"/>
        /// <exception cref="ObjectDisposedException">Throw if the <see cref="DisposableManagedObjectBase.IsDisposed"/>  ty is true.</exception>
        public void StartListeningForNotifications()
        {
            ThrowIfDisposed();
            _socketServer.RequestReceived += ProcessNotificationMessage;
        }

        /// <summary>
        /// Stops listening for broadcast notifications of service availability.
        /// </summary>
        /// <remarks>
        /// <para>Does nothing if this instance is not already listening for notifications.</para>
        /// </remarks>
        /// <seealso cref="StartListeningForNotifications"/>
        /// <seealso cref="DeviceAvailable"/>
        /// <seealso cref="DeviceUnavailable"/>
        /// <exception cref="ObjectDisposedException">Throw if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true.</exception>
        public void StopListeningForNotifications()
        {
            ThrowIfDisposed();
            _socketServer.RequestReceived -= ProcessNotificationMessage;
        }

        /// <summary>
        /// Disposes this object and all internal resources. Stops listening for all network messages.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed, or false is only unmanaged resources should be cleaned up.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_timerLock)
                {
                    _broadcastTimer?.Dispose();
                    _broadcastTimer = null;
                }
                if (_socketServer != null)
                {
                    _socketServer.ResponseReceived -= ProcessSearchResponseMessage;
                    _socketServer.RequestReceived -= ProcessNotificationMessage;
                }
            }
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

            if (!NotificationTypeMatchesFilter(device))
            {
                return isNewDevice;
            }

            DeviceAvailable?.Invoke(this, new DeviceAvailableEventArgs(device, isNewDevice, localIpAddress));

            return isNewDevice;
        }

        private bool NotificationTypeMatchesFilter(DiscoveredSsdpDevice device)
        {
            return string.IsNullOrEmpty(this.NotificationFilter)
                || this.NotificationFilter == "ssdp:all"
                || device.NotificationType == this.NotificationFilter;
        }

        private Task BroadcastDiscoverMessage(TimeSpan mxValue)
        {
            const string Header = "M-SEARCH * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["HOST"] = "239.255.255.250:1900",
                ["USER-AGENT"] = SsdpUserAgent + "\\" + _systemId,
                ["MAN"] = $"\"ssdp:discover\"",
                ["ST"] = "ssdp:all",
                ["MX"] = mxValue.Seconds.ToString(CultureInfo.CurrentCulture)
            };

            var message = BuildMessage(Header, values);

            Task task1 = _socketServer.SendMulticastMessageAsync(message, IPAddress.Any);
            Task task2 = Task.CompletedTask;

            if (_networkManager.IsIP6Enabled)
            {
                values["HOST"] = "[ff01::2]:1900";
                message = BuildMessage(Header, values);
                task2 = _socketServer.SendMulticastMessageAsync(message, IPAddress.IPv6Any);
            }

            return Task.WhenAll(task1, task2);
        }

        private void ProcessSearchResponseMessage(object sender, ResponseReceivedEventArgs e)
        {
            HttpResponseMessage message = e.Message;
            IPAddress localIpAddress = e.LocalIpAddress;

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
                    
                if (AddOrUpdateDiscoveredDevice(device, localIpAddress))
                {
                    _logger.LogDebug("Found DLNA Device : {0} {1}", device.DescriptionLocation, localIpAddress);
                }
            }
        }

        private void ProcessNotificationMessage(object sender, RequestReceivedEventArgs e)
        {
            HttpRequestMessage message = e.Message;
            IPAddress localIpAddress = e.LocalIpAddress;
            if (string.Equals(message.Method.Method, "Notify", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var notificationType = GetFirstHeaderValue("NTS", message.Headers);
            if (string.Equals(notificationType, "ssdp:alive", StringComparison.OrdinalIgnoreCase))
            {
                ProcessAliveNotification(message, localIpAddress);
            }
            else if (string.Equals(notificationType, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                ProcessByeByeNotification(message);
            }
        }

        private void ProcessAliveNotification(HttpRequestMessage message, IPAddress localIpAddress)
        {
            var location = GetFirstHeaderUriValue("Location", message.Headers);
            if (location != null)
            {
                var device = new DiscoveredSsdpDevice(
                    DateTimeOffset.Now,
                    CacheAgeFromHeader(message.Headers.CacheControl),
                    location,
                    GetFirstHeaderValue("NT", message.Headers),
                    GetFirstHeaderValue("USN", message.Headers),
                    message.Headers);

                if (AddOrUpdateDiscoveredDevice(device, localIpAddress))
                {
                    _logger.LogDebug("Alive notification received: {0} ", device.DescriptionLocation);
                }
            }
        }

        private void ProcessByeByeNotification(HttpRequestMessage message)
        {
            var notficationType = GetFirstHeaderValue("NT", message.Headers);
            if (!string.IsNullOrEmpty(notficationType))
            {
                var usn = GetFirstHeaderValue("USN", message.Headers);

                if (!DeviceDied(usn, false))
                {
                    var deadDevice = new DiscoveredSsdpDevice(
                        DateTime.UtcNow,
                        TimeSpan.Zero,
                        null,
                        GetFirstHeaderValue("NT", message.Headers),
                        usn, message.Headers);

                    _logger.LogDebug("Byebye: {0}", deadDevice);

                    if (NotificationTypeMatchesFilter(deadDevice))
                    {
                        DeviceUnavailable?.Invoke(this, new DeviceUnavailableEventArgs(deadDevice, false));
                    }
                }
            }
        }

        private static TimeSpan CacheAgeFromHeader(CacheControlHeaderValue headerValue)
        {
            if (headerValue == null)
            {
                return TimeSpan.Zero;
            }

            return (TimeSpan)(headerValue.MaxAge ?? headerValue.SharedMaxAge ?? TimeSpan.Zero);
        }

        private void RemoveExpiredDevicesFromCache()
        {
            if (this.IsDisposed)
            {
                return;
            }

            DiscoveredSsdpDevice[]? expiredDevices = null;
            lock (_devices)
            {
                expiredDevices = (from device in _devices where device.IsExpired() select device).ToArray();

                foreach (var device in expiredDevices)
                {
                    if (this.IsDisposed)
                    {
                        return;
                    }

                    _devices.Remove(device);
                }
            }

            // Don't do this inside lock because DeviceDied raises an event
            // which means public code may execute during lock and cause
            // problems.
            foreach (var expiredUsn in (from expiredDevice in expiredDevices select expiredDevice.Usn).Distinct())
            {
                if (this.IsDisposed)
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
                    if (this.IsDisposed)
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
                    if (NotificationTypeMatchesFilter(removedDevice))
                    {
                        DeviceUnavailable?.Invoke(this, new DeviceUnavailableEventArgs(removedDevice, expired));
                    }
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
    }
}
