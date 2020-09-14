#pragma warning disable CS1591
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Main;
using Emby.Dlna.PlayTo.Devices;
using Jellyfin.Networking.Manager;
using Jellyfin.Networking.Ssdp;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Emby.Dlna.Net
{
    using SsdpMessage = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public class SsdpServerPublisher : IDisposable
    {
        private const string PnpRootDevice = "pnp:rootdevice";
        private const string UpnpRootDevice = "upnp:rootdevice";
        private const string SsdpNotify = "NOTIFY * HTTP/1.1";
        private readonly ILogger<SsdpServerPublisher> _logger;
        private readonly IList<SsdpRootDevice> _devices;
        private readonly IReadOnlyList<SsdpRootDevice> _readOnlyDevices;
        private readonly INetworkManager _networkManager;
        private readonly Random _random;
        private readonly ISsdpServer _ssdpServer;
        private readonly IDictionary<string, SearchRequest> _recentSearchRequests;
        private Timer? _rebroadcastAliveNotificationsTimer;
        private bool _disposed;

        public SsdpServerPublisher(
            ISsdpServer ssdpServer,
            ILoggerFactory loggerFactory,
            INetworkManager networkManager,
            int aliveMessageInterval)
        {
            _networkManager = networkManager ?? throw new NullReferenceException(nameof(networkManager));
            _ssdpServer = ssdpServer ?? throw new NullReferenceException(nameof(ssdpServer));
            _logger = loggerFactory.CreateLogger<SsdpServerPublisher>();
            _devices = new List<SsdpRootDevice>();
            _readOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_devices);
            _recentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _random = new Random();
            SupportPnpRootDevice = false;
            AliveMessageInterval = aliveMessageInterval;
        }

        /// <summary>
        /// Gets or sets the frequency of the SSDP alive messages (in seconds).
        /// </summary>
        public int AliveMessageInterval { get; set; }

        /// <summary>
        /// Gets returns a read only list of devices being published by this instance.
        /// </summary>
        public IEnumerable<SsdpRootDevice> Devices
        {
            get
            {
                return _readOnlyDevices;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it treats root devices as both upnp:rootdevice and pnp:rootdevice types.
        /// </summary>
        public bool SupportPnpRootDevice { get; set; }

        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server,
        /// making them discoverable to SSDP clients.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AddDevice(SsdpRootDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            ThrowIfDisposed();

            // Must be first, to ensure there will be sockets available.
            _ssdpServer.AddEvent("M-SEARCH", RequestReceived);

            bool wasAdded = false;
            lock (_devices)
            {
                if (!_devices.Contains(device))
                {
                    _devices.Add(device);
                    wasAdded = true;
                }
            }

            if (wasAdded)
            {
                _logger.LogInformation("DLNA server added {0}", device);
                await SendAliveNotifications(device, true).ConfigureAwait(false);
                StartBroadcastingAliveMessages(TimeSpan.FromSeconds(AliveMessageInterval));
            }
        }

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them undiscoverable.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RemoveDevice(SsdpRootDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            bool wasRemoved = false;
            lock (_devices)
            {
                if (_devices.Contains(device))
                {
                    _devices.Remove(device);
                    wasRemoved = true;
                }
            }

            if (wasRemoved)
            {
                _logger.LogInformation("Device Removed {0}", device);

                await SendByeByeNotifications(device, true).ConfigureAwait(false);
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
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _disposed = true;
                    _logger.LogDebug("Disposing instance.");

                    _rebroadcastAliveNotificationsTimer?.Dispose();
                    _rebroadcastAliveNotificationsTimer = null;

                    var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                    Task.WaitAll(tasks);

                    // Must be last, or there won't be any sockets available.
                    _ssdpServer.DeleteEvent("M-SEARCH", RequestReceived);
                }

                _disposed = true;
            }
        }

        private static string GetUsn(string udn, string fullDeviceType)
        {
            return $"{udn}::{fullDeviceType}";
        }

        /// <summary>
        /// Recursive SelectMany - modified from.
        /// https://stackoverflow.com/questions/13409194/is-it-possible-to-implement-a-recursive-selectmany.
        /// </summary>
        /// <typeparam name="T">Type to enumerate.</typeparam>
        /// <param name="collection">Collection to enumerate.</param>
        /// <returns>A flattened representation.</returns>
        private static IEnumerable<T> Flatten<T>(IEnumerable collection)
        {
            if (collection == null)
            {
                yield return (T)Enumerable.Empty<T>();
            }
            else
            {
                foreach (var o in collection)
                {
                    if (o is IEnumerable enumerable && !(o is T))
                    {
                        foreach (T t in Flatten<T>(enumerable))
                        {
                            yield return t;
                        }
                    }
                    else
                    {
                        yield return (T)o;
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Disposed");
            }
        }

        private void StartBroadcastingAliveMessages(TimeSpan interval)
        {
            if (_rebroadcastAliveNotificationsTimer != null)
            {
                _rebroadcastAliveNotificationsTimer.Change(TimeSpan.FromSeconds(5), interval);
            }
            else
            {
                _rebroadcastAliveNotificationsTimer = new Timer(
                    delegate
                    {
                        SendAllAliveNotificationsAsync();
                    },
                    null,
                    TimeSpan.FromSeconds(5),
                    interval);
            }
        }

        private async Task ProcessSearchRequestAsync(int maxWaitInterval, string searchTarget, SsdpEventArgs e)
        {
            if (maxWaitInterval > 120)
            {
                maxWaitInterval = _random.Next(0, 120);
            }

            // Do not block synchronously as that may tie up a threadpool thread for several seconds.
            _ = Task.Delay(_random.Next(16, maxWaitInterval * 1000));

            // Copying devices to local array here to avoid threading issues/enumerator exceptions.
            IEnumerable<SsdpDevice>? devices = null;
            lock (_devices)
            {
                if (string.Equals("ssdp:all", searchTarget, StringComparison.OrdinalIgnoreCase))
                {
                    devices = Flatten<SsdpDevice>(_devices).ToArray();
                }
                else if (string.Equals(UpnpRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) ||
                    (SupportPnpRootDevice && string.Equals(PnpRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase)))
                {
                    devices = _devices.ToArray();
                }
                else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
                {
                    devices = (from device in Flatten<SsdpDevice>(_devices)
                               where string.Equals(device.Uuid, searchTarget.Substring(5), StringComparison.OrdinalIgnoreCase)
                               select device).ToArray();
                }
                else if (searchTarget.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
                {
                    devices = (from device in Flatten<SsdpDevice>(_devices)
                               where string.Equals(device.FullDeviceType, searchTarget, StringComparison.OrdinalIgnoreCase)
                               select device).ToArray();
                }
            }

            if (devices != null)
            {
                var deviceList = devices.ToList();

                SsdpRootDevice rt;

                foreach (var device in deviceList)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    rt = device.ToRootDevice();
                    if (rt.NetAddress.AddressFamily == e.ReceivedFrom.Address.AddressFamily)
                    {
                        var addr = e.ReceivedFrom.Address;

                        if (_ssdpServer.Tracing)
                        {
                            if (_ssdpServer.TracingFilter == null || _ssdpServer.TracingFilter.Equals(addr) || _ssdpServer.TracingFilter.Equals(e.LocalIPAddress))
                            {
                                _logger.LogDebug("<- M-SEARCH: {0} : {1}", addr, searchTarget);
                            }
                        }

                        // Response is sent only when the device in the same subnet, or the the message has come from one of our interfaces.
                        if (rt.NetAddress.Contains(addr) || _networkManager.IsValidInterfaceAddress(addr))
                        {
                            bool isRootDevice = (device as SsdpRootDevice) != null;
                            if (isRootDevice)
                            {
                                await SendSearchResponseAsync(UpnpRootDevice, device, GetUsn(device.Udn, UpnpRootDevice), e).ConfigureAwait(false);

                                if (SupportPnpRootDevice)
                                {
                                    await SendSearchResponseAsync(PnpRootDevice, device, GetUsn(device.Udn, PnpRootDevice), e).ConfigureAwait(false);
                                }
                            }

                            await SendSearchResponseAsync(device.Udn, device, device.Udn, e).ConfigureAwait(false);
                            await SendSearchResponseAsync(device.FullDeviceType, device, GetUsn(device.Udn, device.FullDeviceType), e).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task SendSearchResponseAsync(string searchTarget, SsdpDevice device, string uniqueServiceName, SsdpEventArgs e)
        {
            const string SsdpResponse = "HTTP/1.1 200 OK";

            var rootDevice = device.ToRootDevice();

            var values = new SsdpMessage(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = string.Empty,
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location.ToString(),
            };

            await _ssdpServer.SendSSDP(values, SsdpResponse, e.LocalIPAddress, e.ReceivedFrom).ConfigureAwait(false);
        }

        private bool IsDuplicateSearchRequest(string searchTarget, EndPoint endPoint)
        {
            var isDuplicateRequest = false;

            var newRequest = new SearchRequest(endPoint, searchTarget, DateTime.UtcNow);
            lock (_recentSearchRequests)
            {
                if (_recentSearchRequests.ContainsKey(newRequest.Key))
                {
                    var lastRequest = _recentSearchRequests[newRequest.Key];
                    if (lastRequest.IsOld())
                    {
                        _recentSearchRequests[newRequest.Key] = newRequest;
                    }
                    else
                    {
                        isDuplicateRequest = true;
                    }
                }
                else
                {
                    _recentSearchRequests.Add(newRequest.Key, newRequest);
                    if (_recentSearchRequests.Count > 10)
                    {
                        CleanUpRecentSearchRequestsAsync();
                    }
                }
            }

            return isDuplicateRequest;
        }

        private void CleanUpRecentSearchRequestsAsync()
        {
            lock (_recentSearchRequests)
            {
                foreach (var requestKey in (from r in _recentSearchRequests where r.Value.IsOld() select r.Key).ToArray())
                {
                    _recentSearchRequests.Remove(requestKey);
                }
            }
        }

        /// <summary>
        /// Async timer callback that sends alive NOTIFY ssdp-all notifications.
        /// </summary>
        private async void SendAllAliveNotificationsAsync()
        {
            try
            {
                ThrowIfDisposed();

                // _logger.LogInformation("Begin sending alive notifications for all Devices");

                SsdpRootDevice[] devices;
                lock (_devices)
                {
                    devices = _devices.ToArray();
                }

                foreach (var device in devices)
                {
                    ThrowIfDisposed();

                    await SendAliveNotifications(device, true).ConfigureAwait(false);
                }

                // _logger.LogInformation("Completed transmitting alive notifications for all Devices");
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError("Publisher stopped, exception {0}.", ex.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Advertises the device and associated services with a NOTIFY / ssdp-all.
        /// </summary>
        /// <param name="device">Device to advertise.</param>
        /// <param name="isRoot">True if this is a root device.</param>
        private async Task SendAliveNotifications(SsdpDevice device, bool isRoot)
        {
            if (isRoot)
            {
                await SendAliveNotification(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice)).ConfigureAwait(false);
                if (SupportPnpRootDevice)
                {
                    await SendAliveNotification(device, PnpRootDevice, GetUsn(device.Udn, PnpRootDevice)).ConfigureAwait(false);
                }

                await SendAliveNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            }

            await SendAliveNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Devices)
            {
                await SendAliveNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Advertises the device with a NOTIFY / ssdp-all. Used by SendAliveNotification.
        /// </summary>
        /// <param name="device">Device to advertise.</param>
        /// <param name="notificationType">Device type.</param>
        /// <param name="uniqueServiceName">USN.</param>
        private Task SendAliveNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            var rootDevice = device.ToRootDevice();
            var values = new SsdpMessage(StringComparer.OrdinalIgnoreCase)
            {
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.CurrentCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["LOCATION"] = rootDevice.Location.ToString(),
                ["NTS"] = "ssdp:alive",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName,
            };

            return _ssdpServer.SendMulticastSSDP(values, SsdpNotify, rootDevice.Address);
        }

        private async Task SendByeByeNotifications(SsdpDevice device, bool isRoot)
        {
            if (isRoot)
            {
                await SendByeByeNotification(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice)).ConfigureAwait(false);
                if (SupportPnpRootDevice)
                {
                    await SendByeByeNotification(device, PnpRootDevice, GetUsn(device.Udn, PnpRootDevice)).ConfigureAwait(false);
                }

                await SendByeByeNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            }

            await SendByeByeNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Devices)
            {
                await SendByeByeNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "byebye", Justification = "Correct value for this type of notification in SSDP.")]
        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            var addr = device.ToRootDevice().Address;
            var values = new SsdpMessage(StringComparer.OrdinalIgnoreCase)
            {
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["NTS"] = "ssdp:byebye",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName,
            };
            return _ssdpServer.SendMulticastSSDP(values, SsdpNotify, addr, _disposed ? 1 : _ssdpServer.UdpSendCount);
        }

        private async void RequestReceived(object sender, SsdpEventArgs e)
        {
            const string SsdpInternetGateway = "ssdp:urn:schemas-upnp-org:device:InternetGatewayDevice:";

            ThrowIfDisposed();

            var searchTarget = e.Message["ST"];

            if (searchTarget.StartsWith(SsdpInternetGateway, StringComparison.OrdinalIgnoreCase))
            {
                // If uPNP is running and the message didn't originate from mono - pass these messages to mono.nat. It might want them.
                if (_ssdpServer.IsUPnPActive && !e.Internal)
                {
                    _logger.LogDebug("Passing notify message to Mono.Nat.");
                    NatUtility.ParseMessage(NatProtocol.Upnp, e.LocalIPAddress, e.Raw(), e.ReceivedFrom);
                }

                return;
            }

            if (string.IsNullOrEmpty(searchTarget))
            {
                _logger.LogWarning("Invalid search request received From {0}, Target is null/empty.", e.ReceivedFrom);
                return;
            }

            if (IsDuplicateSearchRequest(searchTarget, e.ReceivedFrom))
            {
                // WriteTrace("Search Request is Duplicate, ignoring.");
                return;
            }

            string mx = e.Message["MX"];
            // Wait on random interval up to MX, as per SSDP spec.
            // Also, as per UPnP 1.1/SSDP spec ignore missing/bank MX header. If over 120, assume random value between 0 and 120.
            // Using 16 as minimum as that's often the minimum system clock frequency anyway.
            if (string.IsNullOrEmpty(mx))
            {
                // Windows Explorer is poorly behaved and doesn't supply an MX header value.
                mx = "1";
            }

            if (!int.TryParse(mx, out int maxWaitInterval) || maxWaitInterval <= 0)
            {
                return;
            }

            await ProcessSearchRequestAsync(maxWaitInterval, searchTarget, e).ConfigureAwait(false);
        }

        private class SearchRequest
        {
            public SearchRequest(EndPoint endPoint, string searchTarget, DateTime received)
            {
                Key = searchTarget + ":" + endPoint.ToString();
                Received = received;
            }

            /// <summary>
            /// Gets or sets the Received.
            /// </summary>
            public DateTime Received { get; set; }

            /// <summary>
            /// Gets the Key.
            /// </summary>
            public string Key { get; }

            public bool IsOld()
            {
                return DateTime.UtcNow.Subtract(Received).TotalMilliseconds > 500;
            }
        }
    }
}
