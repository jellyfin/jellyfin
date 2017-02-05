using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Threading;
using RSSDP;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Allows you to search the network for a particular device, device types, or UPnP service types. Also listenings for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    public abstract class SsdpDeviceLocatorBase : DisposableManagedObjectBase
    {

        #region Fields & Constants

        private List<DiscoveredSsdpDevice> _Devices;
        private ISsdpCommunicationsServer _CommunicationsServer;

        private IList<DiscoveredSsdpDevice> _SearchResults;
        private object _SearchResultsSynchroniser;

        private ITimer _ExpireCachedDevicesTimer;
        private ITimerFactory _timerFactory;

        private static readonly TimeSpan DefaultSearchWaitTime = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected SsdpDeviceLocatorBase(ISsdpCommunicationsServer communicationsServer, ITimerFactory timerFactory)
        {
            if (communicationsServer == null) throw new ArgumentNullException("communicationsServer");

            _CommunicationsServer = communicationsServer;
            _timerFactory = timerFactory;
            _CommunicationsServer.ResponseReceived += CommsServer_ResponseReceived;

            _SearchResultsSynchroniser = new object();
            _Devices = new List<DiscoveredSsdpDevice>();
        }

        #endregion

        #region Events

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

        #endregion

        #region Public Methods

        #region Search Overloads

        /// <summary>
        /// Performs a search for all devices using the default search timeout.
        /// </summary>
        /// <returns>A task whose result is an <see cref="IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
        public Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync(CancellationToken cancellationToken)
        {
            return SearchAsync(SsdpConstants.SsdpDiscoverAllSTHeader, DefaultSearchWaitTime, cancellationToken);
        }

        /// <summary>
        /// Performs a search for the specified search target (criteria) and default search timeout.
        /// </summary>
        /// <param name="searchTarget">The criteria for the search. Value can be;
        /// <list type="table">
        /// <item><term>Root devices</term><description>upnp:rootdevice</description></item>
        /// <item><term>Specific device by UUID</term><description>uuid:&lt;device uuid&gt;</description></item>
        /// <item><term>Device type</term><description>Fully qualified device type starting with urn: i.e urn:schemas-upnp-org:Basic:1</description></item>
        /// </list>
        /// </param>
        /// <returns>A task whose result is an <see cref="IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
        public Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync(string searchTarget)
        {
            return SearchAsync(searchTarget, DefaultSearchWaitTime, CancellationToken.None);
        }

        /// <summary>
        /// Performs a search for all devices using the specified search timeout.
        /// </summary>
        /// <param name="searchWaitTime">The amount of time to wait for network responses to the search request. Longer values will likely return more devices, but increase search time. A value between 1 and 5 seconds is recommended by the UPnP 1.1 specification, this method requires the value be greater 1 second if it is not zero. Specify TimeSpan.Zero to return only devices already in the cache.</param>
        /// <returns>A task whose result is an <see cref="IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
        public Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync(TimeSpan searchWaitTime)
        {
            return SearchAsync(SsdpConstants.SsdpDiscoverAllSTHeader, searchWaitTime, CancellationToken.None);
        }

        public async Task<IEnumerable<DiscoveredSsdpDevice>> SearchAsync(string searchTarget, TimeSpan searchWaitTime, CancellationToken cancellationToken)
        {
            if (searchTarget == null) throw new ArgumentNullException("searchTarget");
            if (searchTarget.Length == 0) throw new ArgumentException("searchTarget cannot be an empty string.", "searchTarget");
            if (searchWaitTime.TotalSeconds < 0) throw new ArgumentException("searchWaitTime must be a positive time.");
            if (searchWaitTime.TotalSeconds > 0 && searchWaitTime.TotalSeconds <= 1) throw new ArgumentException("searchWaitTime must be zero (if you are not using the result and relying entirely in the events), or greater than one second.");

            ThrowIfDisposed();

            if (_SearchResults != null) throw new InvalidOperationException("Search already in progress. Only one search at a time is allowed.");
            _SearchResults = new List<DiscoveredSsdpDevice>();

            // If searchWaitTime == 0 then we are only going to report unexpired cached items, not actually do a search.
            if (searchWaitTime > TimeSpan.Zero)
                await BroadcastDiscoverMessage(searchTarget, SearchTimeToMXValue(searchWaitTime), cancellationToken).ConfigureAwait(false);

            lock (_SearchResultsSynchroniser)
            {
                foreach (var device in GetUnexpiredDevices().Where(NotificationTypeMatchesFilter))
                {
                    DeviceFound(device, false, null);
                }
            }

            if (searchWaitTime != TimeSpan.Zero)
                await Task.Delay(searchWaitTime, cancellationToken).ConfigureAwait(false);

            IEnumerable<DiscoveredSsdpDevice> retVal = null;

            try
            {
                lock (_SearchResultsSynchroniser)
                {
                    retVal = _SearchResults;
                    _SearchResults = null;
                }

                RemoveExpiredDevicesFromCache();
            }
            finally
            {
                var server = _CommunicationsServer;
                try
                {
                    if (server != null) // In case we were disposed while searching.
                        server.StopListeningForResponses();
                }
                catch (ObjectDisposedException) { }
            }

            return retVal;
        }

        #endregion

        /// <summary>
        /// Starts listening for broadcast notifications of service availability.
        /// </summary>
        /// <remarks>
        /// <para>When called the system will listen for 'alive' and 'byebye' notifications. This can speed up searching, as well as provide dynamic notification of new devices appearing on the network, and previously discovered devices disappearing.</para>
        /// </remarks>
        /// <seealso cref="StopListeningForNotifications"/>
        /// <seealso cref="DeviceAvailable"/>
        /// <seealso cref="DeviceUnavailable"/>
        /// <exception cref="System.ObjectDisposedException">Throw if the <see cref="DisposableManagedObjectBase.IsDisposed"/>  ty is true.</exception>
        public void StartListeningForNotifications()
        {
            ThrowIfDisposed();

            _CommunicationsServer.RequestReceived -= CommsServer_RequestReceived;
            _CommunicationsServer.RequestReceived += CommsServer_RequestReceived;
            _CommunicationsServer.BeginListeningForBroadcasts();
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
        /// <exception cref="System.ObjectDisposedException">Throw if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true.</exception>
        public void StopListeningForNotifications()
        {
            ThrowIfDisposed();

            _CommunicationsServer.RequestReceived -= CommsServer_RequestReceived;
        }

        /// <summary>
        /// Raises the <see cref="DeviceAvailable"/> event.
        /// </summary>
        /// <seealso cref="DeviceAvailable"/>
        protected virtual void OnDeviceAvailable(DiscoveredSsdpDevice device, bool isNewDevice, IpAddressInfo localIpAddress)
        {
            if (this.IsDisposed) return;

            var handlers = this.DeviceAvailable;
            if (handlers != null)
                handlers(this, new DeviceAvailableEventArgs(device, isNewDevice)
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
            if (this.IsDisposed) return;

            var handlers = this.DeviceUnavailable;
            if (handlers != null)
                handlers(this, new DeviceUnavailableEventArgs(device, expired));
        }

        #endregion

        #region Public Properties

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

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes this object and all internal resources. Stops listening for all network messages.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed, or false is only unmanaged resources should be cleaned up.</param>
        protected override void Dispose(bool disposing)
        {

            if (disposing)
            {
                var timer = _ExpireCachedDevicesTimer;
                if (timer != null)
                    timer.Dispose();

                var commsServer = _CommunicationsServer;
                _CommunicationsServer = null;
                if (commsServer != null)
                {
                    commsServer.ResponseReceived -= this.CommsServer_ResponseReceived;
                    commsServer.RequestReceived -= this.CommsServer_RequestReceived;
                    if (!commsServer.IsShared)
                        commsServer.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        #region Discovery/Device Add

        private void AddOrUpdateDiscoveredDevice(DiscoveredSsdpDevice device, IpAddressInfo localIpAddress)
        {
            bool isNewDevice = false;
            lock (_Devices)
            {
                var existingDevice = FindExistingDeviceNotification(_Devices, device.NotificationType, device.Usn);
                if (existingDevice == null)
                {
                    _Devices.Add(device);
                    isNewDevice = true;
                }
                else
                {
                    _Devices.Remove(existingDevice);
                    _Devices.Add(device);
                }
            }

            DeviceFound(device, isNewDevice, localIpAddress);
        }

        private void DeviceFound(DiscoveredSsdpDevice device, bool isNewDevice, IpAddressInfo localIpAddress)
        {
            // Don't raise the event if we've already done it for a cached
            // version of this device, and the cached version isn't
            // "significantly" different, i.e location and cachelifetime 
            // haven't changed.
            var raiseEvent = false;

            if (!NotificationTypeMatchesFilter(device)) return;

            lock (_SearchResultsSynchroniser)
            {
                if (_SearchResults != null)
                {
                    var existingDevice = FindExistingDeviceNotification(_SearchResults, device.NotificationType, device.Usn);
                    if (existingDevice == null)
                    {
                        _SearchResults.Add(device);
                        raiseEvent = true;
                    }
                    else
                    {
                        if (existingDevice.DescriptionLocation != device.DescriptionLocation || existingDevice.CacheLifetime != device.CacheLifetime)
                        {
                            _SearchResults.Remove(existingDevice);
                            _SearchResults.Add(device);
                            raiseEvent = true;
                        }
                    }
                }
                else
                    raiseEvent = true;
            }

            if (raiseEvent)
                OnDeviceAvailable(device, isNewDevice, localIpAddress);
        }

        private bool NotificationTypeMatchesFilter(DiscoveredSsdpDevice device)
        {
            return String.IsNullOrEmpty(this.NotificationFilter)
                || this.NotificationFilter == SsdpConstants.SsdpDiscoverAllSTHeader
                || device.NotificationType == this.NotificationFilter;
        }

        #endregion

        #region Network Message Processing

        private Task BroadcastDiscoverMessage(string serviceType, TimeSpan mxValue, CancellationToken cancellationToken)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            values["HOST"] = "239.255.255.250:1900";
            values["USER-AGENT"] = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";
            //values["X-EMBY-SERVERID"] = _appHost.SystemId;

            values["MAN"] = "\"ssdp:discover\"";

            // Search target
            values["ST"] = "ssdp:all";

            // Seconds to delay response
            values["MX"] = "3";

            var header = "M-SEARCH * HTTP/1.1";

            var message = SsdpHelper.BuildMessage(header, values);

            return _CommunicationsServer.SendMulticastMessage(message, cancellationToken);
        }

        private void ProcessSearchResponseMessage(HttpResponseMessage message, IpAddressInfo localIpAddress)
        {
            if (!message.IsSuccessStatusCode) return;

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

        private void ProcessNotificationMessage(HttpRequestMessage message, IpAddressInfo localIpAddress)
        {
            if (String.Compare(message.Method.Method, "Notify", StringComparison.OrdinalIgnoreCase) != 0) return;

            var notificationType = GetFirstHeaderStringValue("NTS", message);
            if (String.Compare(notificationType, SsdpConstants.SsdpKeepAliveNotification, StringComparison.OrdinalIgnoreCase) == 0)
                ProcessAliveNotification(message, localIpAddress);
            else if (String.Compare(notificationType, SsdpConstants.SsdpByeByeNotification, StringComparison.OrdinalIgnoreCase) == 0)
                ProcessByeByeNotification(message);
        }

        private void ProcessAliveNotification(HttpRequestMessage message, IpAddressInfo localIpAddress)
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

                ResetExpireCachedDevicesTimer();
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

                    if (NotificationTypeMatchesFilter(deadDevice))
                        OnDeviceUnavailable(deadDevice, false);
                }

                ResetExpireCachedDevicesTimer();
            }
        }

        private void ResetExpireCachedDevicesTimer()
        {
            if (IsDisposed) return;

            if (_ExpireCachedDevicesTimer == null)
                _ExpireCachedDevicesTimer = _timerFactory.Create(this.ExpireCachedDevices, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            _ExpireCachedDevicesTimer.Change(60000, System.Threading.Timeout.Infinite);
        }

        private void ExpireCachedDevices(object state)
        {
            RemoveExpiredDevicesFromCache();
        }

        #region Header/Message Processing Utilities

        private static string GetFirstHeaderStringValue(string headerName, HttpResponseMessage message)
        {
            string retVal = null;
            IEnumerable<string> values;
            if (message.Headers.Contains(headerName))
            {
                message.Headers.TryGetValues(headerName, out values);
                if (values != null)
                    retVal = values.FirstOrDefault();
            }

            return retVal;
        }

        private static string GetFirstHeaderStringValue(string headerName, HttpRequestMessage message)
        {
            string retVal = null;
            IEnumerable<string> values;
            if (message.Headers.Contains(headerName))
            {
                message.Headers.TryGetValues(headerName, out values);
                if (values != null)
                    retVal = values.FirstOrDefault();
            }

            return retVal;
        }

        private static Uri GetFirstHeaderUriValue(string headerName, HttpRequestMessage request)
        {
            string value = null;
            IEnumerable<string> values;
            if (request.Headers.Contains(headerName))
            {
                request.Headers.TryGetValues(headerName, out values);
                if (values != null)
                    value = values.FirstOrDefault();
            }

            Uri retVal;
            Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out retVal);
            return retVal;
        }

        private static Uri GetFirstHeaderUriValue(string headerName, HttpResponseMessage response)
        {
            string value = null;
            IEnumerable<string> values;
            if (response.Headers.Contains(headerName))
            {
                response.Headers.TryGetValues(headerName, out values);
                if (values != null)
                    value = values.FirstOrDefault();
            }

            Uri retVal;
            Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out retVal);
            return retVal;
        }

        private static TimeSpan CacheAgeFromHeader(System.Net.Http.Headers.CacheControlHeaderValue headerValue)
        {
            if (headerValue == null) return TimeSpan.Zero;

            return (TimeSpan)(headerValue.MaxAge ?? headerValue.SharedMaxAge ?? TimeSpan.Zero);
        }

        #endregion

        #endregion

        #region Expiry and Device Removal

        private void RemoveExpiredDevicesFromCache()
        {
            if (this.IsDisposed) return;

            DiscoveredSsdpDevice[] expiredDevices = null;
            lock (_Devices)
            {
                expiredDevices = (from device in _Devices where device.IsExpired() select device).ToArray();

                foreach (var device in expiredDevices)
                {
                    if (this.IsDisposed) return;

                    _Devices.Remove(device);
                }
            }

            // Don't do this inside lock because DeviceDied raises an event
            // which means public code may execute during lock and cause
            // problems.
            foreach (var expiredUsn in (from expiredDevice in expiredDevices select expiredDevice.Usn).Distinct())
            {
                if (this.IsDisposed) return;

                DeviceDied(expiredUsn, true);
            }
        }

        private IEnumerable<DiscoveredSsdpDevice> GetUnexpiredDevices()
        {
            lock (_Devices)
            {
                return (from device in _Devices where !device.IsExpired() select device).ToArray();
            }
        }

        private bool DeviceDied(string deviceUsn, bool expired)
        {
            IEnumerable<DiscoveredSsdpDevice> existingDevices = null;
            lock (_Devices)
            {
                existingDevices = FindExistingDeviceNotifications(_Devices, deviceUsn);
                foreach (var existingDevice in existingDevices)
                {
                    if (this.IsDisposed) return true;

                    _Devices.Remove(existingDevice);
                }
            }

            if (existingDevices != null && existingDevices.Any())
            {
                lock (_SearchResultsSynchroniser)
                {
                    if (_SearchResults != null)
                    {
                        var resultsToRemove = (from result in _SearchResults where result.Usn == deviceUsn select result).ToArray();
                        foreach (var result in resultsToRemove)
                        {
                            if (this.IsDisposed) return true;

                            _SearchResults.Remove(result);
                        }
                    }
                }

                foreach (var removedDevice in existingDevices)
                {
                    if (NotificationTypeMatchesFilter(removedDevice))
                        OnDeviceUnavailable(removedDevice, expired);
                }

                return true;
            }

            return false;
        }

        #endregion

        private static TimeSpan SearchTimeToMXValue(TimeSpan searchWaitTime)
        {
            if (searchWaitTime.TotalSeconds < 2 || searchWaitTime == TimeSpan.Zero)
                return OneSecond;
            else
                return searchWaitTime.Subtract(OneSecond);
        }

        private static DiscoveredSsdpDevice FindExistingDeviceNotification(IEnumerable<DiscoveredSsdpDevice> devices, string notificationType, string usn)
        {
            return (from d in devices where d.NotificationType == notificationType && d.Usn == usn select d).FirstOrDefault();
        }

        private static IEnumerable<DiscoveredSsdpDevice> FindExistingDeviceNotifications(IList<DiscoveredSsdpDevice> devices, string usn)
        {
            return (from d in devices where d.Usn == usn select d).ToArray();
        }

        #endregion

        #region Event Handlers

        private void CommsServer_ResponseReceived(object sender, ResponseReceivedEventArgs e)
        {
            ProcessSearchResponseMessage(e.Message, e.LocalIpAddress);
        }

        private void CommsServer_RequestReceived(object sender, RequestReceivedEventArgs e)
        {
            ProcessNotificationMessage(e.Message, e.LocalIpAddress);
        }

        #endregion

    }
}