#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Ssdp
{
    /// <summary>
    /// Searches the network for a particular device, device types, or UPnP service types.
    /// Listens for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    /// <remarks>
    /// Part of this code are taken from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public class SsdpLocator : IDisposable
    {
        private readonly object _timerLock;
        private readonly object _deviceLock;
        private readonly ISsdpServer _ssdpServer;
        private readonly ILogger _logger;
        private readonly TimeSpan _defaultSearchWaitTime;
        private readonly TimeSpan _oneSecond;
        private readonly string[] _ssdpFilter;
        private readonly bool _enableBroadcast;
        private Timer? _broadcastTimer;
        private bool _disposed;
        private bool _listening;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpLocator"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="interfaces">A <see cref="Collection{IPObject}"/> of interface addresses to listen on.</param>
        /// <param name="filter">Array of Ssdp types which this instance should process.</param>
        /// <param name="activelySearch">True if this instance actively uses broadcasts to locate devices.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        public SsdpLocator(ILogger logger, Collection<IPObject> interfaces, string[] filter, bool activelySearch, INetworkManager networkManager)
        {
            _timerLock = new object();
            _deviceLock = new object();
            _ssdpFilter = filter;
            _logger = logger;
            _defaultSearchWaitTime = TimeSpan.FromSeconds(4);
            _oneSecond = TimeSpan.FromSeconds(1);
            Devices = new List<DiscoveredSsdpDevice>();
            _ssdpServer = SsdpServer.GetOrCreateInstance(logger, interfaces, networkManager);
            _ssdpServer.AddEvent("HTTP/1.1 200 OK", ProcessSearchResponseMessage);
            _ssdpServer.AddEvent("NOTIFY", ProcessNotificationMessage);
            _enableBroadcast = activelySearch;
        }

        /// <summary>
        /// Gets the list of the devices located.
        /// </summary>
        protected List<DiscoveredSsdpDevice> Devices { get; }

        /// <summary>
        /// Gets or sets the interval between broadcasts.
        /// </summary>
        protected int Interval { get; set; }

        /// <summary>
        /// Starts the periodic broadcasting of M-SEARCH requests.
        /// </summary>
        public virtual void Start()
        {
            if (!_enableBroadcast)
            {
                return;
            }

            var dueTime = TimeSpan.FromSeconds(5);
            var period = TimeSpan.FromSeconds(Interval);
            lock (_timerLock)
            {
                _listening = true;
                if (_broadcastTimer == null)
                {
                    _broadcastTimer = new Timer(OnBroadcastTimerCallback, null, dueTime, period);
                    OnBroadcastTimerCallback(0);
                }
                else
                {
                    _broadcastTimer.Change(dueTime, period);
                }
            }
        }

        /// <summary>
        /// Disposes this object instance and all internally managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this object and all internal resources. Stops listening for all network messages.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed, or false is only unmanaged resources should be cleaned up.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _disposed = true;
            _ssdpServer.DeleteEvent("HTTP/1.1 200 OK", ProcessSearchResponseMessage);
            _ssdpServer.DeleteEvent("NOTIFY", ProcessNotificationMessage);

            _logger.LogDebug("Disposing instance.");
            lock (_timerLock)
            {
                _broadcastTimer?.Dispose();
                _broadcastTimer = null;
            }
        }

        /// <summary>
        /// Called when a new device is detected.
        /// </summary>
        /// <param name="isNewDevice">True if the device is new.</param>
        /// <param name="args">Device details.</param>
        protected virtual void DeviceDiscoveredEvent(bool isNewDevice, SsdpDeviceInfo args)
        {
        }

        /// <summary>
        /// Called when a device has signaled it's leaving.
        /// Note: Not all leaving devices will trigger this event.
        /// </summary>
        /// <param name="args">Device details.</param>
        protected virtual void DeviceLeftEvent(SsdpDeviceInfo args)
        {
        }

        /// <summary>
        /// Searches the list of known devices and returns the one matching the criteria.
        /// </summary>
        /// <param name="devices">List of devices to search.</param>
        /// <param name="notificationType">Notification type criteria.</param>
        /// <param name="usn">USN criteria.</param>
        /// <returns>Device if located, or null if not.</returns>
        private static DiscoveredSsdpDevice? FindExistingDevice(IEnumerable<DiscoveredSsdpDevice> devices, string notificationType, string usn)
        {
            return devices.FirstOrDefault(d => string.Equals(d.NotificationType, notificationType, StringComparison.Ordinal)
              && string.Equals(d.Usn, usn, StringComparison.Ordinal));
        }

        /// <summary>
        /// Searches the list of known devices and returns the one matching the criteria.
        /// </summary>
        /// <param name="devices">List of devices to search.</param>
        /// <param name="usn">USN criteria.</param>
        /// <returns>A <see cref="List{DiscoveredSsdpDevice}"/> containing the matching devices.</returns>
        private static List<DiscoveredSsdpDevice> FindExistingDevices(IEnumerable<DiscoveredSsdpDevice> devices, string usn)
        {
            return devices.Where(d => string.Equals(d.Usn, usn, StringComparison.Ordinal)).ToList();
        }

        /// <summary>
        /// Removes old entries from the cache and transmits a discovery message.
        /// </summary>
        /// <param name="state">Not used.</param>
        private async void OnBroadcastTimerCallback(object? state)
        {
            try
            {
                RemoveExpiredDevicesFromCache();
                await BroadcastDiscoverMessage(SearchTimeToMxValue(_defaultSearchWaitTime)).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Do nothing.
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchAsync failed.");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Adds or updates the discovered device list.
        /// </summary>
        /// <param name="device">Device to add.</param>
        /// <param name="address">IP Address of the device.</param>
        /// <returns>True if the device is a new discovery.</returns>
        private bool AddOrUpdateDiscoveredDevice(DiscoveredSsdpDevice device, IPAddress address)
        {
            bool isNewDevice = false;
            lock (_deviceLock)
            {
                var existingDevice = FindExistingDevice(Devices, device.NotificationType, device.Usn);
                if (existingDevice == null)
                {
                    Devices.Add(device);
                    isNewDevice = true;
                }
                else
                {
                    Devices.Remove(existingDevice);
                    Devices.Add(device);
                }
            }

            DeviceDiscoveredEvent(isNewDevice, new SsdpDeviceInfo(device.DescriptionLocation, device.Headers, address));

            return isNewDevice;
        }

        /// <summary>
        /// Returns true if the device type passed is one that we want.
        /// </summary>
        /// <param name="device">Device to check.</param>
        /// <returns>Result of the operation.</returns>
        private bool SsdpTypeMatchesFilter(DiscoveredSsdpDevice device)
        {
            return _ssdpFilter.Any(m => device.NotificationType.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Broadcasts a SSDP M-SEARCH request.
        /// </summary>
        /// <param name="mxValue">Mx value for the packet.</param>
        private Task BroadcastDiscoverMessage(TimeSpan mxValue)
        {
            const string SsdpSearch = "M-SEARCH * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ST"] = "ssdp:all",
                ["MAN"] = "\"ssdp:discover\"",
                ["MX"] = mxValue.Seconds.ToString(CultureInfo.CurrentCulture),
            };

            return _ssdpServer.SendMulticastSSDP(values, SsdpSearch);
        }

        /// <summary>
        /// Processes the search response message.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">A <see cref="SsdpEventArgs"/> containing details of the event.</param>
        private void ProcessSearchResponseMessage(object? sender, SsdpEventArgs e)
        {
            if (!_listening || _disposed)
            {
                return;
            }

            var location = e.Message["LOCATION"];
            if (string.IsNullOrEmpty(location))
            {
                return;
            }

            var device = new DiscoveredSsdpDevice(DateTimeOffset.Now, "ST", e.Message);

            if (!SsdpTypeMatchesFilter(device))
            {
                // Filtered type - not interested.
                return;
            }

            if (AddOrUpdateDiscoveredDevice(device, e.ReceivedFrom.Address))
            {
                _logger.LogDebug("Found DLNA Device : {DescriptionLocation}", device.DescriptionLocation);
            }
        }

        /// <summary>
        /// Processes a notification message.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">A <see cref="SsdpEventArgs"/> containing details of the event.</param>
        private void ProcessNotificationMessage(object? sender, SsdpEventArgs e)
        {
            if (!e.Message.ContainsKey("LOCATION"))
            {
                return;
            }

            var device = new DiscoveredSsdpDevice(DateTimeOffset.Now, "NT", e.Message);
            if (!SsdpTypeMatchesFilter(device))
            {
                // Filtered type - not interested.
                return;
            }

            IPAddress localIpAddress = e.LocalIpAddress;
            var notificationType = e.Message["NTS"];
            if (device.DescriptionLocation != null)
            {
                if (string.Equals(notificationType, "ssdp:alive", StringComparison.OrdinalIgnoreCase))
                {
                    // Process Alive Notification.
                    if (!AddOrUpdateDiscoveredDevice(device, localIpAddress))
                    {
                        return;
                    }

                    if (_ssdpServer.Tracing && (_ssdpServer.TracingFilter == null || _ssdpServer.TracingFilter.Equals(localIpAddress)))
                    {
                        _logger.LogDebug("<- ssdpalive : {DescriptionLocation} ", device.DescriptionLocation);
                    }

                    return;
                }
            }

            if (string.IsNullOrEmpty(device.NotificationType) || string.Equals(notificationType, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Process ByeBye Notification.
            if (DeviceDied(device.Usn))
            {
                return;
            }

            if (_ssdpServer.Tracing && (_ssdpServer.TracingFilter == null || _ssdpServer.TracingFilter.Equals(localIpAddress)))
            {
                _logger.LogDebug("Byebye: {Device}", device);
            }

            var args = new SsdpDeviceInfo(device.DescriptionLocation, device.Headers, localIpAddress);
            DeviceLeftEvent(args);
        }

        /// <summary>
        /// Removes expired devices from the cache.
        /// </summary>
        private void RemoveExpiredDevicesFromCache()
        {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly: Syntax checker cannot cope with a null array x[]?
            DiscoveredSsdpDevice[]? expiredDevices;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

            lock (_deviceLock)
            {
                expiredDevices = (from device in Devices where device.IsExpired() select device).ToArray();

                foreach (var device in expiredDevices)
                {
                    Devices.Remove(device);
                }
            }

            // Don't do this inside lock because DeviceDied raises an event which means public code may execute during lock and cause problems.
            foreach (var expiredUsn in (from expiredDevice in expiredDevices select expiredDevice.Usn).Distinct())
            {
                DeviceDied(expiredUsn);
            }
        }

        /// <summary>
        /// Removes a device from the cache.
        /// </summary>
        /// <param name="deviceUsn">USN of the device.</param>
        /// <returns>True if the operation succeeded.</returns>
        private bool DeviceDied(string deviceUsn)
        {
            List<DiscoveredSsdpDevice>? existingDevices;
            lock (_deviceLock)
            {
                existingDevices = FindExistingDevices(Devices, deviceUsn);
                foreach (var existingDevice in existingDevices)
                {
                    Devices.Remove(existingDevice);
                }
            }

            if (existingDevices.Count == 0)
            {
                return false;
            }

            foreach (var removedDevice in existingDevices)
            {
                var args = new SsdpDeviceInfo(removedDevice.DescriptionLocation, removedDevice.Headers, IPAddress.Any);

                DeviceLeftEvent(args);
            }

            return true;
        }

        /// <summary>
        /// Reduces the wait time by one.
        /// </summary>
        /// <param name="searchWaitTime">Timespan to reduce.</param>
        /// <returns>The resultant timespan.</returns>
        private TimeSpan SearchTimeToMxValue(TimeSpan searchWaitTime)
        {
            if (searchWaitTime.TotalSeconds < 2 || searchWaitTime == TimeSpan.Zero)
            {
                return _oneSecond;
            }

            return searchWaitTime.Subtract(_oneSecond);
        }
    }
}
