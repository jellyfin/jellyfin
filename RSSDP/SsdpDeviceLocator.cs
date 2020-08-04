#pragma warning disable CA1303 // Do not pass literals as localized parameters

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Allows you to search the network for a particular device, device types, or UPnP service types. Also listenings for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    public class SsdpDeviceLocator : DisposableManagedObjectBase
    {
        private readonly List<DiscoveredSsdpDevice> _devices;
        private ISsdpCommunicationsServer _communicationsServer;
        private Timer _broadcastTimer;
        private readonly object _timerLock = new object();
        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;
        private readonly TimeSpan _defaultSearchWaitTime = TimeSpan.FromSeconds(4);
        private readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SsdpDeviceLocator(ISsdpCommunicationsServer communicationsServer, ILogger logger, INetworkManager networkManager)
        {
            _communicationsServer = communicationsServer ?? throw new ArgumentNullException(nameof(communicationsServer));
            _communicationsServer.ResponseReceived += CommsServer_ResponseReceived;
            _networkManager = networkManager;
            _devices = new List<DiscoveredSsdpDevice>();
            _logger = logger;
        }

        /// <summary>
        /// Raised for when
        /// <list type="bullet">
        /// <item>An 'alive' notification is received that a device, regardless of whether or not that device is not already in the cache or has previously raised this event.</item>
        /// <item>For each item found during a device <see cref="SearchAsync()"/> (cached or not), allowing clients to respond to found devices before the entire search is complete.</item>
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
                await SearchAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchAsync failed.");
            }
        }

        /// <summary>
        /// Performs a search for all devices using the default search timeout.
        /// </summary>
        private Task SearchAsync()
        {
            return SearchAsync(_defaultSearchWaitTime);
        }
        
        private Task SearchAsync(TimeSpan searchWaitTime)
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

            this.DeviceAvailable?.Invoke(this, new DeviceAvailableEventArgs(device, isNewDevice)
            {
                LocalIpAddress = localIpAddress
            });
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

            this.DeviceUnavailable?.Invoke(this, new DeviceUnavailableEventArgs(device, expired));
        }

        /// <summary>
        /// Sets or returns a string containing the filter for notifications. Notifications not matching the filter will not raise the <see cref="ISsdpDeviceLocator.DeviceAvailable"/> or <see cref="ISsdpDeviceLocator.DeviceUnavailable"/> events.
        /// </summary>
        /// <remarks>
        /// <para>Device alive/byebye notifications whose NT header does not match this filter value will still be captured and cached internally, but will not raise events about device availability. Usually used with either a device type of uuid NT header value.</para>
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
        public string NotificationFilter
        {
            get;
            set;
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

            // Only fire event if we haven't seen this device before.
            if (isNewDevice)
            {
                DeviceFound(device, isNewDevice, localIpAddress);
            }

            return isNewDevice;
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
            return String.IsNullOrEmpty(this.NotificationFilter)
                || this.NotificationFilter == SsdpConstants.SsdpDiscoverAllSTHeader
                || device.NotificationType == this.NotificationFilter;
        }

        private Task BroadcastDiscoverMessage(TimeSpan mxValue)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["HOST"] = $"{SsdpConstants.MulticastLocalAdminAddress}:{SsdpConstants.MulticastPort}",
                ["USER-AGENT"] = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2",
                //values["X-EMBY-SERVERID"] = _appHost.SystemId;
                ["MAN"] = $"\"{SsdpConstants.SsdpDiscoverMessage}\"",
                // Search target
                ["ST"] = SsdpConstants.SsdpDiscoverAllSTHeader,
                // Seconds to delay response
                ["MX"] = mxValue.Seconds.ToString(CultureInfo.CurrentCulture)
            };

            var header = "M-SEARCH * HTTP/1.1";
            var message = BuildMessage(header, values);

            if (!_networkManager.IsIP6Enabled)
            {
                //_logger.LogInformation("Sending IPv4 broadcast on LAN interfaces.");

                IEnumerable<Task> tasks = from intf in _networkManager.GetInternalBindAddresses()
                                          select _communicationsServer.SendMulticastMessage(message, intf.Address);
                return Task.WhenAll(tasks);
            }

            values["HOST"] = $"{SsdpConstants.MulticastLocalAdminAddressV6}:{SsdpConstants.MulticastPort}";
            var message2 = BuildMessage(header, values);

            //_logger.LogInformation("Sending IPv6 multicast and IPv4 broadcast.");
            IEnumerable<Task> ip4Tasks = from intf in _networkManager.GetInternalBindAddresses()
                                      where (intf.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                      select _communicationsServer.SendMulticastMessage(message, intf.Address);

            IEnumerable<Task> ip6Tasks = from intf in _networkManager.GetInternalBindAddresses()
                                      where (intf.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                                      select _communicationsServer.SendMulticastMessage(message2, intf.Address);


            return Task.WhenAll(ip4Tasks.Union(ip6Tasks));                            
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

                if (AddOrUpdateDiscoveredDevice(device, localIpAddress))
                {
                    _logger.LogDebug("Search Response: {0} {1}", localIpAddress, device);
                }
            }
        }

        private void ProcessNotificationMessage(HttpRequestMessage message, IPAddress localIpAddress)
        {
            if (String.Compare(message.Method.Method, "Notify", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return;
            }

            var notificationType = GetFirstHeaderStringValue("NTS", message);
            if (String.Compare(notificationType, SsdpConstants.SsdpKeepAliveNotification, StringComparison.OrdinalIgnoreCase) == 0)
            {
                ProcessAliveNotification(message, localIpAddress);
            }
            else if (String.Compare(notificationType, SsdpConstants.SsdpByeByeNotification, StringComparison.OrdinalIgnoreCase) == 0)
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

                if (AddOrUpdateDiscoveredDevice(device, localIpAddress))
                {
                    _logger.LogDebug("Alive notification: {0} ", device.DescriptionLocation);
                }
            }
        }

        private void ProcessByeByeNotification(HttpRequestMessage message)
        {
            var notficationType = GetFirstHeaderStringValue("NT", message);
            if (!String.IsNullOrEmpty(notficationType))
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
                    _logger.LogDebug("Byebye: {0}", deadDevice);
                    if (NotificationTypeMatchesFilter(deadDevice))
                    {
                        OnDeviceUnavailable(deadDevice, false);
                    }
                }
            }
        }

        private static string GetFirstHeaderStringValue(string headerName, HttpResponseMessage message)
        {
            string retVal = null;
            if (message.Headers.Contains(headerName))
            {
                message.Headers.TryGetValues(headerName, out IEnumerable<string> values);
                if (values != null)
                {
                    retVal = values.FirstOrDefault();
                }
            }

            return retVal;
        }

        private static string GetFirstHeaderStringValue(string headerName, HttpRequestMessage message)
        {
            string retVal = null;
            if (message.Headers.Contains(headerName))
            {
                message.Headers.TryGetValues(headerName, out IEnumerable<string> values);
                if (values != null)
                {
                    retVal = values.FirstOrDefault();
                }
            }

            return retVal;
        }

        private static Uri GetFirstHeaderUriValue(string headerName, HttpRequestMessage request)
        {
            string value = null;
            if (request.Headers.Contains(headerName))
            {
                request.Headers.TryGetValues(headerName, out IEnumerable<string> values);
                if (values != null)
                {
                    value = values.FirstOrDefault();
                }
            }

            Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri retVal);
            return retVal;
        }

        private static Uri GetFirstHeaderUriValue(string headerName, HttpResponseMessage response)
        {
            string value = null;
            if (response.Headers.Contains(headerName))
            {
                response.Headers.TryGetValues(headerName, out IEnumerable<string> values);
                if (values != null)
                {
                    value = values.FirstOrDefault();
                }
            }

            Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri retVal);
            return retVal;
        }

        private static TimeSpan CacheAgeFromHeader(System.Net.Http.Headers.CacheControlHeaderValue headerValue)
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

            DiscoveredSsdpDevice[] expiredDevices = null;
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
            List<DiscoveredSsdpDevice> existingDevices = null;
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
                        OnDeviceUnavailable(removedDevice, expired);
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

        private static DiscoveredSsdpDevice FindExistingDeviceNotification(IEnumerable<DiscoveredSsdpDevice> devices, string notificationType, string usn)
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

        private static List<DiscoveredSsdpDevice> FindExistingDeviceNotifications(IList<DiscoveredSsdpDevice> devices, string usn)
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
