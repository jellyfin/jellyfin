using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Allows you to search the network for a particular device, device types, or UPnP service types. Also listenings for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    public class SsdpDeviceLocator : DisposableManagedObjectBase
    {
        private List<DiscoveredSsdpDevice> _devices;
        private ISsdpCommunicationsServer _communicationsServer;
        private Timer _broadcastTimer;
        private object _timerLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpDeviceLocator"/> class.
        /// </summary>
        public SsdpDeviceLocator(ISsdpCommunicationsServer communicationsServer)
        {
            if (communicationsServer == null)
            {
                throw new ArgumentNullException(nameof(communicationsServer));
            }

            _communicationsServer = communicationsServer;
            _communicationsServer.ResponseReceived += CommsServer_ResponseReceived;

            _devices = new List<DiscoveredSsdpDevice>();
        }

        /// <summary>
        /// Raised for when
        /// <list type="bullet">
        /// <item>An 'alive' notification is received that a device, regardless of whether or not that device is not already in the cache or has previously raised this event.</item>
        /// <item>For each item found during a device <see cref="SearchAsync(System.Threading.CancellationToken)"/> (cached or not), allowing clients to respond to found devices before the entire search is complete.</item>
        /// <item>Only if the notification type matches the <see cref="NotificationFilter"/> property. By default the filter is null, meaning all notifications raise events (regardless of ant </item>
        /// </list>
        /// <para>This event may be raised from a background thread, if interacting with UI or other objects with specific thread affinity invoking to the relevant thread is required.</para>
        /// </summary>
        /// <seealso cref="NotificationFilter"/>
        /// <seealso cref="DeviceUnavailable"/>
        /// <seealso cref="StartListeningForNotifications"/>
        /// <seealso cref="StopListeningForNotifications"/>
        public event EventHandler<DeviceAvailableEventArgs> DeviceAvailable;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        /// <remarks>
        /// <para>Devices *should* broadcast these types of notifications, but not all devices do and sometimes (in the event of power loss for example) it might not be possible for a device to do so. You should also implement error handling when trying to contact a device, even if RSSDP is reporting that device as available.</para>
        /// <para>This event is only raised if the notification type matches the <see cref="NotificationFilter"/> property. A null or empty string for the <see cref="NotificationFilter"/> will be treated as no filter and raise the event for all notifications.</para>
        /// <para>The <see cref="DeviceUnavailableEventArgs.DiscoveredDevice"/> property may contain either a fully complete <see cref="DiscoveredSsdpDevice"/> instance, or one containing just a USN and NotificationType property. Full information is available if the device was previously discovered and cached, but only partial information if a byebye notification was received for a previously unseen or expired device.</para>
        /// <para>This event may be raised from a background thread, if interacting with UI or other objects with specific thread affinity invoking to the relevant thread is required.</para>
        /// </remarks>
        /// <seealso cref="NotificationFilter"/>
        /// <seealso cref="DeviceAvailable"/>
        /// <seealso cref="StartListeningForNotifications"/>
        /// <seealso cref="StopListeningForNotifications"/>
        public event EventHandler<DeviceUnavailableEventArgs> DeviceUnavailable;

        /// <summary>
        /// Gets or sets a string containing the filter for notifications. Notifications not matching the filter will not raise the <see cref="ISsdpDeviceLocator.DeviceAvailable"/> or <see cref="ISsdpDeviceLocator.DeviceUnavailable"/> events.
        /// </summary>
        /// <remarks>
        /// <para>Device alive/byebye notifications whose NT header does not match this filter value will still be captured and cached internally, but will not raise events about device availability. Usually used with either a device type of uuid NT header value.</para>
        /// <para>If the value is null or empty string then, all notifications are reported.</para>
        /// <para>Example filters follow.</para>
        /// <example>upnp:rootdevice</example>
        /// <example>urn:schemas-upnp-org:device:WANDevice:1</example>
        /// <example>uuid:9F15356CC-95FA-572E-0E99-85B456BD3012</example>
        /// </remarks>
        /// <seealso cref="ISsdpDeviceLocator.DeviceAvailable"/>
        /// <seealso cref="ISsdpDeviceLocator.DeviceUnavailable"/>
        /// <seealso cref="ISsdpDeviceLocator.StartListeningForNotifications"/>
        /// <seealso cref="ISsdpDeviceLocator.StopListeningForNotifications"/>
        public string NotificationFilter { get; set; }

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

        public void DisposeBroadcastTimer()
        {
            lock (_timerLock)
            {
                if (_broadcastTimer != null)
                {
                    _broadcastTimer.Dispose();
                    _broadcastTimer = null;
                }
            }
        }

        private async void OnBroadcastTimerCallback(object state)
        {
            StartListeningForNotifications();
            RemoveExpiredDevicesFromCache();

            try
            {
                await SearchAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Performs a search for all devices using the default search timeout.
        /// </summary>
        private Task SearchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return BroadcastDiscoverMessage(cancellationToken);
        }

        /// <summary>
        /// Starts listening for broadcast notifications of service availability.
        /// </summary>
        /// <remarks>
        /// <para>When called the system will listen for 'alive' and 'byebye' notifications. This can speed up searching, as well as provide dynamic notification of new devices appearing on the network, and previously discovered devices disappearing.</para>
        /// </remarks>
        /// <seealso cref="StopListeningForNotifications"/>
        /// <seealso cref="DeviceAvailable"/>
        /// <seealso cref="DeviceUnavailable"/>
        /// <exception cref="ObjectDisposedException">Throw if the <see cref="DisposableManagedObjectBase.IsDisposed"/>  ty is true.</exception>
        public void StartListeningForNotifications()
        {
            ThrowIfDisposed();

            _communicationsServer.RequestReceived -= CommsServer_RequestReceived;
            _communicationsServer.RequestReceived += CommsServer_RequestReceived;
            _communicationsServer.BeginListeningForBroadcasts();
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

            _communicationsServer.RequestReceived -= CommsServer_RequestReceived;
        }

        /// <summary>
        /// Raises the <see cref="DeviceAvailable"/> event.
        /// </summary>
        /// <seealso cref="DeviceAvailable"/>
        protected virtual void OnDeviceAvailable(DiscoveredSsdpDevice device, bool isNewDevice, IPAddress localIpAddress)
        {
            if (this.IsDisposed)
            {
                return;
            }

            var handlers = this.DeviceAvailable;
            if (handlers != null)
            {
                handlers(this, new DeviceAvailableEventArgs(device, isNewDevice)
                {
                    LocalIpAddress = localIpAddress
                });
            }
        }

        /// <summary>
        /// Raises the <see cref="DeviceUnavailable"/> event.
        /// </summary>
        /// <param name="device">A <see cref="DiscoveredSsdpDevice"/> representing the device that is no longer available.</param>
        /// <param name="expired">True if the device expired from the cache without being renewed, otherwise false to indicate the device explicitly notified us it was being shutdown.</param>
        /// <seealso cref="DeviceUnavailable"/>
        protected virtual void OnDeviceUnavailable(DiscoveredSsdpDevice device, bool expired)
        {
            if (this.IsDisposed)
            {
                return;
            }

            var handlers = this.DeviceUnavailable;
            if (handlers != null)
            {
                handlers(this, new DeviceUnavailableEventArgs(device, expired));
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
                DisposeBroadcastTimer();

                var commsServer = _communicationsServer;
                _communicationsServer = null;
                if (commsServer != null)
                {
                    commsServer.ResponseReceived -= this.CommsServer_ResponseReceived;
                    commsServer.RequestReceived -= this.CommsServer_RequestReceived;
                }
            }
        }

        private void AddOrUpdateDiscoveredDevice(DiscoveredSsdpDevice device, IPAddress localIpAddress)
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

            DeviceFound(device, isNewDevice, localIpAddress);
        }

        private void DeviceFound(DiscoveredSsdpDevice device, bool isNewDevice, IPAddress localIpAddress)
        {
            if (!NotificationTypeMatchesFilter(device))
            {
                return;
            }

            OnDeviceAvailable(device, isNewDevice, localIpAddress);
        }

        private bool NotificationTypeMatchesFilter(DiscoveredSsdpDevice device)
        {
            return string.IsNullOrEmpty(this.NotificationFilter)
                || this.NotificationFilter == SsdpConstants.SsdpDiscoverAllStHeader
                || device.NotificationType == this.NotificationFilter;
        }

        private Task BroadcastDiscoverMessage(CancellationToken cancellationToken)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            values["HOST"] = "239.255.255.250:1900";
            values["USER-AGENT"] = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";
            // values["X-EMBY-SERVERID"] = _appHost.SystemId;

            values["MAN"] = "\"ssdp:discover\"";

            // Search target
            values["ST"] = "ssdp:all";

            // Seconds to delay response
            values["MX"] = "3";

            var header = "M-SEARCH * HTTP/1.1";

            var message = BuildMessage(header, values);

            return _communicationsServer.SendMulticastMessage(message, null, cancellationToken);
        }

        private void ProcessSearchResponseMessage(HttpResponseMessage message, IPAddress localIpAddress)
        {
            if (!message.IsSuccessStatusCode)
            {
                return;
            }

            var location = GetFirstHeaderUriValue("Location", message);
            if (location != null)
            {
                var device = new DiscoveredSsdpDevice()
                {
                    DescriptionLocation = location,
                    Usn = GetFirstHeaderStringValue("USN", message),
                    NotificationType = GetFirstHeaderStringValue("ST", message),
                    CacheLifetime = CacheAgeFromHeader(message.Headers.CacheControl),
                    AsAt = DateTimeOffset.Now,
                    ResponseHeaders = message.Headers
                };

                AddOrUpdateDiscoveredDevice(device, localIpAddress);
            }
        }

        private void ProcessNotificationMessage(HttpRequestMessage message, IPAddress localIpAddress)
        {
            if (string.Compare(message.Method.Method, "Notify", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return;
            }

            var notificationType = GetFirstHeaderStringValue("NTS", message);
            if (string.Compare(notificationType, SsdpConstants.SsdpKeepAliveNotification, StringComparison.OrdinalIgnoreCase) == 0)
            {
                ProcessAliveNotification(message, localIpAddress);
            }
            else if (string.Compare(notificationType, SsdpConstants.SsdpByeByeNotification, StringComparison.OrdinalIgnoreCase) == 0)
            {
                ProcessByeByeNotification(message);
            }
        }

        private void ProcessAliveNotification(HttpRequestMessage message, IPAddress localIpAddress)
        {
            var location = GetFirstHeaderUriValue("Location", message);
            if (location != null)
            {
                var device = new DiscoveredSsdpDevice()
                {
                    DescriptionLocation = location,
                    Usn = GetFirstHeaderStringValue("USN", message),
                    NotificationType = GetFirstHeaderStringValue("NT", message),
                    CacheLifetime = CacheAgeFromHeader(message.Headers.CacheControl),
                    AsAt = DateTimeOffset.Now,
                    ResponseHeaders = message.Headers
                };

                AddOrUpdateDiscoveredDevice(device, localIpAddress);
            }
        }

        private void ProcessByeByeNotification(HttpRequestMessage message)
        {
            var notficationType = GetFirstHeaderStringValue("NT", message);
            if (!string.IsNullOrEmpty(notficationType))
            {
                var usn = GetFirstHeaderStringValue("USN", message);

                if (!DeviceDied(usn, false))
                {
                    var deadDevice = new DiscoveredSsdpDevice()
                    {
                        AsAt = DateTime.UtcNow,
                        CacheLifetime = TimeSpan.Zero,
                        DescriptionLocation = null,
                        NotificationType = GetFirstHeaderStringValue("NT", message),
                        Usn = usn,
                        ResponseHeaders = message.Headers
                    };

                    if (NotificationTypeMatchesFilter(deadDevice))
                    {
                        OnDeviceUnavailable(deadDevice, false);
                    }
                }
            }
        }

        private string GetFirstHeaderStringValue(string headerName, HttpResponseMessage message)
        {
            string retVal = null;
            IEnumerable<string> values;
            if (message.Headers.Contains(headerName))
            {
                message.Headers.TryGetValues(headerName, out values);
                if (values != null)
                {
                    retVal = values.FirstOrDefault();
                }
            }

            return retVal;
        }

        private string GetFirstHeaderStringValue(string headerName, HttpRequestMessage message)
        {
            string retVal = null;
            IEnumerable<string> values;
            if (message.Headers.Contains(headerName))
            {
                message.Headers.TryGetValues(headerName, out values);
                if (values != null)
                {
                    retVal = values.FirstOrDefault();
                }
            }

            return retVal;
        }

        private Uri GetFirstHeaderUriValue(string headerName, HttpRequestMessage request)
        {
            string value = null;
            IEnumerable<string> values;
            if (request.Headers.Contains(headerName))
            {
                request.Headers.TryGetValues(headerName, out values);
                if (values != null)
                {
                    value = values.FirstOrDefault();
                }
            }

            Uri retVal;
            Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out retVal);
            return retVal;
        }

        private Uri GetFirstHeaderUriValue(string headerName, HttpResponseMessage response)
        {
            string value = null;
            IEnumerable<string> values;
            if (response.Headers.Contains(headerName))
            {
                response.Headers.TryGetValues(headerName, out values);
                if (values != null)
                {
                    value = values.FirstOrDefault();
                }
            }

            Uri retVal;
            Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out retVal);
            return retVal;
        }

        private TimeSpan CacheAgeFromHeader(System.Net.Http.Headers.CacheControlHeaderValue headerValue)
        {
            if (headerValue == null)
            {
                return TimeSpan.Zero;
            }

            return headerValue.MaxAge ?? headerValue.SharedMaxAge ?? TimeSpan.Zero;
        }

        private void RemoveExpiredDevicesFromCache()
        {
            if (this.IsDisposed)
            {
                return;
            }

            DiscoveredSsdpDevice[] expiredDevices;
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
            List<DiscoveredSsdpDevice> existingDevices;
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

            if (existingDevices.Count > 0)
            {
                foreach (var removedDevice in existingDevices)
                {
                    if (NotificationTypeMatchesFilter(removedDevice))
                    {
                        OnDeviceUnavailable(removedDevice, expired);
                    }
                }

                return true;
            }

            return false;
        }

        private DiscoveredSsdpDevice FindExistingDeviceNotification(IEnumerable<DiscoveredSsdpDevice> devices, string notificationType, string usn)
        {
            foreach (var d in devices)
            {
                if (d.NotificationType == notificationType && d.Usn == usn)
                {
                    return d;
                }
            }

            return null;
        }

        private List<DiscoveredSsdpDevice> FindExistingDeviceNotifications(IList<DiscoveredSsdpDevice> devices, string usn)
        {
            var list = new List<DiscoveredSsdpDevice>();

            foreach (var d in devices)
            {
                if (d.Usn == usn)
                {
                    list.Add(d);
                }
            }

            return list;
        }

        private void CommsServer_ResponseReceived(object sender, ResponseReceivedEventArgs e)
        {
            ProcessSearchResponseMessage(e.Message, e.LocalIpAddress);
        }

        private void CommsServer_RequestReceived(object sender, RequestReceivedEventArgs e)
        {
            ProcessNotificationMessage(e.Message, e.LocalIpAddress);
        }
    }
}
