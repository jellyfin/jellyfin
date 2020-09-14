using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Jellyfin.Networking.Ssdp
{
    using SddpMessage = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Searches the network for a particular device, device types, or UPnP service types.
    /// Listenings for broadcast notifications of device availability and raises events to indicate changes in status.
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
        private readonly INetworkManager _networkManager;
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
        /// <param name="networkManager">The <see cref="NetworkManager"/> instance.</param>
        /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
        /// <param name="applicationHost">The <see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="filter">Array of Ssdp types which this instance should process.</param>
        /// <param name="activelySearch">True if this instance actively uses broadcasts to locate devices.</param>
        public SsdpLocator(
            ILogger logger,
            INetworkManager networkManager,
            IConfigurationManager configurationManager,
            IServerApplicationHost applicationHost,
            string[] filter,
            bool activelySearch)
        {
            _networkManager = networkManager;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultSearchWaitTime = TimeSpan.FromSeconds(4);
            _oneSecond = TimeSpan.FromSeconds(1);
            _timerLock = new object();
            _deviceLock = new object();
            Configuration = configurationManager;
            Devices = new List<DiscoveredSsdpDevice>();
            _ssdpServer = SsdpServer.GetOrCreateInstance(_networkManager, configurationManager, logger, applicationHost);
            _ssdpFilter = filter;
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
        /// Gets the Configuration manager instance to use.
        /// </summary>
        protected IConfigurationManager Configuration { get; }

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
            if (disposing)
            {
                _disposed = true;
                if (_ssdpServer != null)
                {
                    _ssdpServer.DeleteEvent("HTTP/1.1 200 OK", ProcessSearchResponseMessage);
                    _ssdpServer.DeleteEvent("NOTIFY", ProcessNotificationMessage);
                }

                _logger.LogDebug("Disposing instance.");
                lock (_timerLock)
                {
                    _broadcastTimer?.Dispose();
                    _broadcastTimer = null;
                }
            }
        }

        /// <summary>
        /// Called when a new device is detected.
        /// </summary>
        /// <param name="isNewDevice">True if the device is new.</param>
        /// <param name="args">Device details.</param>
        protected virtual void DeviceDiscoveredEvent(bool isNewDevice, GenericEventArgs<UpnpDeviceInfo> args)
        {
        }

        /// <summary>
        /// Called when a device has signalled it's leaving.
        /// Note: Not all leaving devices will trigger this event.
        /// </summary>
        /// <param name="args">Device details.</param>
        protected virtual void DeviceLeftEvent(GenericEventArgs<UpnpDeviceInfo> args)
        {
        }

        private static DiscoveredSsdpDevice? FindExistingDevice(IEnumerable<DiscoveredSsdpDevice> devices, string notificationType, string usn)
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

        private static List<DiscoveredSsdpDevice> FindExistingDevices(IList<DiscoveredSsdpDevice> devices, string usn)
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

        private async void OnBroadcastTimerCallback(object? state)
        {
            try
            {
                RemoveExpiredDevicesFromCache();
                await BroadcastDiscoverMessage(SearchTimeToMXValue(_defaultSearchWaitTime)).ConfigureAwait(false);
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

            var args = new GenericEventArgs<UpnpDeviceInfo>(
                new UpnpDeviceInfo
                {
                    Location = device.DescriptionLocation,
                    Headers = device.Headers,
                    LocalIpAddress = address
                });

            DeviceDiscoveredEvent(isNewDevice, args);

            return isNewDevice;
        }

        /// <summary>
        /// Returns true if the device type passed is one that we want.
        /// </summary>
        /// <param name="device">Device to check.</param>
        /// <returns>Result of the operation.</returns>
        private bool SsdpTypeMatchesFilter(DiscoveredSsdpDevice device)
        {
            return _ssdpFilter.Where(m => device.NotificationType.StartsWith(m, StringComparison.OrdinalIgnoreCase)).Any();
        }

        private Task BroadcastDiscoverMessage(TimeSpan mxValue)
        {
            const string SsdpSearch = "M-SEARCH * HTTP/1.1";

            var values = new SddpMessage(StringComparer.OrdinalIgnoreCase)
            {
                ["ST"] = "ssdp:all",
                ["MAN"] = "\"ssdp:discover\"",
                ["MX"] = mxValue.Seconds.ToString(CultureInfo.CurrentCulture),
            };

            return _ssdpServer.SendMulticastSSDP(values, SsdpSearch);
        }

        private void ProcessSearchResponseMessage(object? sender, SsdpEventArgs e)
        {
            if (!_listening || _disposed)
            {
                return;
            }

            var location = e.Message["LOCATION"];
            if (!string.IsNullOrEmpty(location))
            {
                var device = new DiscoveredSsdpDevice(DateTimeOffset.Now, "ST", e.Message);

                if (!SsdpTypeMatchesFilter(device))
                {
                    // Filtered type - not interested.
                    return;
                }

                if (AddOrUpdateDiscoveredDevice(device, e.ReceivedFrom.Address))
                {
                    _logger.LogDebug("Found DLNA Device : {0}", device.DescriptionLocation);
                }
            }
        }

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

            IPAddress localIpAddress = e.LocalIPAddress;
            var notificationType = e.Message["NTS"];
            if (device.DescriptionLocation != null)
            {
                if (string.Equals(notificationType, "ssdp:alive", StringComparison.OrdinalIgnoreCase))
                {
                    // Process Alive Notification.
                    if (AddOrUpdateDiscoveredDevice(device, localIpAddress))
                    {
                        if (_ssdpServer.Tracing)
                        {
                            if (_ssdpServer.TracingFilter == null || _ssdpServer.TracingFilter.Equals(localIpAddress))
                            {
                                _logger.LogDebug("<- ssdpalive : {0} ", device.DescriptionLocation);
                            }
                        }
                    }

                    return;
                }
            }

            if (!string.IsNullOrEmpty(device.NotificationType) && string.Equals(notificationType, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                // Process ByeBye Notification.
                if (!DeviceDied(device.Usn))
                {
                    if (_ssdpServer.Tracing)
                    {
                        if (_ssdpServer.TracingFilter == null || _ssdpServer.TracingFilter.Equals(localIpAddress))
                        {
                            _logger.LogDebug("Byebye: {0}", device);
                        }
                    }

                    var args = new GenericEventArgs<UpnpDeviceInfo>(
                        new UpnpDeviceInfo
                        {
                            Location = device.DescriptionLocation,
                            Headers = device.Headers,
                            LocalIpAddress = localIpAddress
                        });

                    DeviceLeftEvent(args);
                }
            }
        }

        private void RemoveExpiredDevicesFromCache()
        {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly: Syntax checker cannot cope with a null array x[]?
            DiscoveredSsdpDevice[]? expiredDevices = null;
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

        private bool DeviceDied(string deviceUsn)
        {
            List<DiscoveredSsdpDevice>? existingDevices = null;
            lock (_deviceLock)
            {
                existingDevices = FindExistingDevices(Devices, deviceUsn);
                foreach (var existingDevice in existingDevices)
                {
                    Devices.Remove(existingDevice);
                }
            }

            if (existingDevices != null && existingDevices.Count > 0)
            {
                foreach (var removedDevice in existingDevices)
                {
                    var args = new GenericEventArgs<UpnpDeviceInfo>(
                        new UpnpDeviceInfo
                        {
                            Location = removedDevice.DescriptionLocation,
                            Headers = removedDevice.Headers,
                            LocalIpAddress = IPAddress.Any
                        });

                    DeviceLeftEvent(args);
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
