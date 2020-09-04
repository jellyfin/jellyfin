#pragma warning disable CS1591
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo.Devices;
using Jellyfin.Data.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Emby.Dlna.Net
{
    using SddpMessage = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Uses DeviceDiscovery to allow you to search the network for a particular device, device types, or UPnP service types.
    /// Listenings for broadcast notifications of device availability and raises events to indicate changes in status.
    /// </summary>
    /// <remarks>
    /// Part of this code are taken from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public class SsdpPlayToLocator : IDisposable, ISsdpPlayToLocator
    {
        private readonly List<DiscoveredSsdpDevice> _devices;
        private readonly IServerConfigurationManager _config;
        private readonly ISsdpServer _ssdpServer;
        private readonly object _timerLock;
        private readonly ILogger<SsdpPlayToLocator> _logger;
        private readonly TimeSpan _defaultSearchWaitTime;
        private readonly TimeSpan _oneSecond;
        private readonly string[] _ssdpFilter;
        private Timer? _broadcastTimer;
        private bool _disposed;
        private bool _listening;

        public SsdpPlayToLocator(ISsdpServer ssdpServer, IServerConfigurationManager config, ILogger<SsdpPlayToLocator> logger)
        {
            _ssdpServer = ssdpServer ?? throw new ArgumentNullException(nameof(ssdpServer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultSearchWaitTime = TimeSpan.FromSeconds(4);
            _oneSecond = TimeSpan.FromSeconds(1);
            _timerLock = new object();
            _devices = new List<DiscoveredSsdpDevice>();
            _config = config;
            _ssdpFilter = new string[]
            {
                // "urn:schemas-upnp-org:device:MediaServer:",
                "urn:schemas-upnp-org:device:MediaRenderer:",
                // "urn:schemas-upnp-org:device:MediaPlayer:",
                "urn:schemas-upnp-org:device:InternetGatewayDevice:"
            };

            _ssdpServer.AddEvent("HTTP/1.1 200 OK", ProcessSearchResponseMessage);
            _ssdpServer.AddEvent("NOTIFY", ProcessNotificationMessage);
        }

        /// <summary>
        /// Raised when a new device is discovered.
        /// </summary>
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceDiscovered;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceLeft;

        /// <summary>
        /// Starts the periodic broadcasting of M-SEARCH requests.
        /// </summary>
        public void Start()
        {
            var dueTime = TimeSpan.FromSeconds(5);
            var period = TimeSpan.FromSeconds(_config.GetDlnaConfiguration().ClientDiscoveryIntervalSeconds);
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

            var args = new GenericEventArgs<UpnpDeviceInfo>(
                new UpnpDeviceInfo
                {
                    Location = device.DescriptionLocation,
                    Headers = device.Headers,
                    LocalIpAddress = address
                });

            DeviceDiscovered?.Invoke(this, args);

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
                ["ST"] = "ssdp:all", // ssdp:rootdevice
                ["MAN"] = "\"ssdp:discover\"",
                ["MX"] = mxValue.Seconds.ToString(CultureInfo.CurrentCulture),
            };

            return _ssdpServer.SendMulticastSSDP(values, SsdpSearch);
        }

        private void ProcessSearchResponseMessage(object sender, SsdpEventArgs e)
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

        private void ProcessNotificationMessage(object sender, SsdpEventArgs e)
        {
            // const string SsdpInternetGateway = "ssdp:urn:schemas-upnp-org:device:InternetGatewayDevice:";

            if (!_listening || _disposed)
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
                // if (notificationType.StartsWith(SsdpInternetGateway, StringComparison.OrdinalIgnoreCase))
                // {
                    // If uPNP is running and the message didn't originate from mono - pass these messages to mono.nat. It might want them.
                    // if (_ssdpServer.IsUPnPActive && !e.Internal)
                    // {
                        // _logger.LogDebug("Passing NOTIFY message to Mono.Nat.");
                        // NatUtility.ParseMessage(NatProtocol.Upnp, localIpAddress, e.Raw(), e.ReceivedFrom);
                        // return;
                    // }

                    // return;
                // }

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

                    DeviceLeft?.Invoke(this, args);
                }
            }
        }

        private void RemoveExpiredDevicesFromCache()
        {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly: Syntax checker cannot cope with a null array x[]?
            DiscoveredSsdpDevice[]? expiredDevices = null;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

            lock (_devices)
            {
                expiredDevices = (from device in _devices where device.IsExpired() select device).ToArray();

                foreach (var device in expiredDevices)
                {
                    _devices.Remove(device);
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
            lock (_devices)
            {
                existingDevices = FindExistingDeviceNotifications(_devices, deviceUsn);
                foreach (var existingDevice in existingDevices)
                {
                    _devices.Remove(existingDevice);
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
                    DeviceLeft?.Invoke(this, args);
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
