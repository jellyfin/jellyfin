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
using Emby.Dlna.Rssdp;
using Emby.Dlna.Rssdp.Devices;
using Emby.Dlna.Rssdp.EventArgs;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Rssdp
{
    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    public class SsdpDevicePublisher : SsdpInfrastructure, ISsdpDevicePublisher
    {
        private const string SsdpUserAgent = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";
        private const string PnpRootDevice = "pnp:rootdevice";
        private const string UpnpRootDevice = "upnp:rootdevice";

        private readonly ILogger _logger;
        private readonly string _server;
        private readonly string _systemId;
        private readonly IList<SsdpRootDevice> _devices;
        private readonly IReadOnlyList<SsdpRootDevice> _readOnlyDevices;
        private readonly INetworkManager _networkManager;
        private readonly Random _random;
        private readonly SocketServer _socketServer;
        private readonly IDictionary<string, SearchRequest> _recentSearchRequests;
        private Timer? _rebroadcastAliveNotificationsTimer;

        public SsdpDevicePublisher(
            SocketServer socketServer,
            IServerApplicationHost applicationHost,
            ILoggerFactory loggerFactory,
            INetworkManager networkManager,
            int aliveMessageInterval)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _socketServer = socketServer ?? throw new ArgumentNullException(nameof(socketServer));
            _logger = loggerFactory.CreateLogger<SsdpDevicePublisher>();
            _systemId = applicationHost?.SystemId ?? throw new ArgumentNullException(nameof(applicationHost));

            _server = MediaBrowser.Common.System.OperatingSystem.Name + "/" +
                Environment.OSVersion.VersionString + " UPnP/1.0 RSSDP/1.0";

            _devices = new List<SsdpRootDevice>();
            _readOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_devices);
            _recentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _random = new Random();

            SupportPnpRootDevice = true;

            _socketServer.RequestReceived += RequestReceived;
            _socketServer.UsageCount++;
            AliveMessageInterval = aliveMessageInterval;
        }

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
        /// Recursive SelectMany - modified from.
        /// https://stackoverflow.com/questions/13409194/is-it-possible-to-implement-a-recursive-selectmany.
        /// </summary>
        /// <typeparam name="T">Type to enumerate.</typeparam>
        /// <param name="collection">Collection to enumerate.</param>
        /// <returns>A flattened representation.</returns>
        public static IEnumerable<T> Flatten<T>(IEnumerable collection)
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

        public void StartBroadcastingAliveMessages(TimeSpan interval)
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
                try
                {
                    _logger.LogInformation("Device Added {0}", Expand(device));

                    await SendAliveNotifications(device, true).ConfigureAwait(false);

                    StartBroadcastingAliveMessages(TimeSpan.FromSeconds(AliveMessageInterval));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending Alive messages.");
                }
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
                _logger.LogInformation("Device Removed {0}", Expand(device));

                await SendByeByeNotifications(device, true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops listening for requests, stops sending periodic broadcasts, disposes all internal resources.
        /// </summary>
        /// <param name="disposing">Is disposing.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRebroadcastTimer();

                _socketServer.RequestReceived -= RequestReceived;

                var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                Task.WaitAll(tasks);
            }
        }

        private static string GetUsn(string udn, string fullDeviceType)
        {
            return $"{udn}::{fullDeviceType}";
        }

        private async Task ProcessSearchRequest(string mx, string searchTarget, IPEndPoint remoteEndPoint, IPAddress localIpAddress)
        {
            if (string.IsNullOrEmpty(searchTarget))
            {
                _logger.LogWarning("Invalid search request received From {0}, Target is null/empty.", remoteEndPoint);
                return;
            }

            _logger.LogDebug("Search Request Received From {0}, Target = {1}", remoteEndPoint.ToString(), searchTarget);

            if (IsDuplicateSearchRequest(searchTarget, remoteEndPoint))
            {
                // WriteTrace("Search Request is Duplicate, ignoring.");
                return;
            }

            if (searchTarget.StartsWith("ssdp:urn:schemas-upnp-org:device:InternetGatewayDevice:", StringComparison.OrdinalIgnoreCase))
            {
                Mono.Nat.
            }

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

            if (maxWaitInterval > 120)
            {
                maxWaitInterval = _random.Next(0, 120);
            }

            // Do not block synchronously as that may tie up a threadpool thread for several seconds.

            _ = Task.Delay(_random.Next(16, maxWaitInterval * 1000));

            await ProcessSearchRequestAsync(searchTarget, remoteEndPoint, localIpAddress).ConfigureAwait(false);
        }

        private async Task ProcessSearchRequestAsync(string searchTarget, IPEndPoint remoteEndPoint, IPAddress localIpAddress)
        {
            // Copying devices to local array here to avoid threading issues/enumerator exceptions.
            IEnumerable<SsdpDevice>? devices = null;
            lock (_devices)
            {
                if (string.Equals("ssdp:all", searchTarget, StringComparison.OrdinalIgnoreCase))
                {
                    devices = Flatten<SsdpDevice>(_devices).ToArray();
                }
                else if (string.Equals(UpnpRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) ||
                    (this.SupportPnpRootDevice && string.Equals(PnpRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase)))
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

                foreach (var device in deviceList)
                {
                    var rt = device.ToRootDevice();

                    // Response is sent only when the device in the same subnet, or the are private address on the local device.
                    if (_networkManager.IsInSameSubnet(rt.Address, rt.SubnetMask, remoteEndPoint.Address) ||
                        _networkManager.OnSameMachine(rt.Address, remoteEndPoint.Address))
                    {
                        await SendDeviceSearchResponsesAsync(device, remoteEndPoint, localIpAddress).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                // _logger.LogInformation("Sending 0 search responses.");
            }
        }

        private async Task SendDeviceSearchResponsesAsync(SsdpDevice device, IPEndPoint endPoint, IPAddress localIP)
        {
            bool isRootDevice = (device as SsdpRootDevice) != null;
            if (isRootDevice)
            {
                await SendSearchResponseAsync(UpnpRootDevice, device, GetUsn(device.Udn, UpnpRootDevice), endPoint, localIP).ConfigureAwait(false);

                if (this.SupportPnpRootDevice)
                {
                    await SendSearchResponseAsync(PnpRootDevice, device, GetUsn(device.Udn, PnpRootDevice), endPoint, localIP).ConfigureAwait(false);
                }
            }

            await SendSearchResponseAsync(device.Udn, device, device.Udn, endPoint, localIP).ConfigureAwait(false);
            await SendSearchResponseAsync(device.FullDeviceType, device, GetUsn(device.Udn, device.FullDeviceType), endPoint, localIP).ConfigureAwait(false);
        }

        private async Task SendSearchResponseAsync(string searchTarget, SsdpDevice device, string uniqueServiceName, IPEndPoint endPoint, IPAddress localIP)
        {
            var rootDevice = device.ToRootDevice();

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = string.Empty,
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["SERVER"] = _server,
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location.ToString()
            };

            var message = BuildMessage("HTTP/1.1 200 OK", values);

            await _socketServer.SendMessageAsync(System.Text.Encoding.UTF8.GetBytes(message), endPoint, localIP).ConfigureAwait(false);
            _logger.LogInformation("Sent search response to {0} : {1}", endPoint, device);
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
        /// Async timer callback that sends alive notifications - (hence the use of async void).
        /// </summary>
        private async void SendAllAliveNotificationsAsync()
        {
            try
            {
                if (IsDisposed)
                {
                    return;
                }

                _logger.LogInformation("Begin sending alive notifications for all Devices");

                SsdpRootDevice[] devices;
                lock (_devices)
                {
                    devices = _devices.ToArray();
                }

                foreach (var device in devices)
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    await SendAliveNotifications(device, true).ConfigureAwait(false);
                }

                _logger.LogInformation("Completed transmitting alive notifications for all Devices");
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError("Publisher stopped, exception {0}.", ex.Message);
                Dispose();
            }
        }

        private async Task SendAliveNotifications(SsdpDevice device, bool isRoot)
        {
            if (isRoot)
            {
                await SendAliveNotification(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice)).ConfigureAwait(false);
                if (this.SupportPnpRootDevice)
                {
                    await SendAliveNotification(device, PnpRootDevice, GetUsn(device.Udn, PnpRootDevice)).ConfigureAwait(false);
                }
            }

            await SendAliveNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            await SendAliveNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Devices)
            {
                await SendAliveNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        private Task SendAliveNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            const string Header = "NOTIFY * HTTP / 1.1";

            var rootDevice = device.ToRootDevice();
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = "239.255.255.250:1900",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.CurrentCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["LOCATION"] = rootDevice.Location.ToString(),
                ["SERVER"] = _server,
                ["NTS"] = "ssdp:alive",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage(Header, values);

            Task task1 = _socketServer.SendMulticastMessageAsync(message, rootDevice.Address);
            Task task2 = Task.CompletedTask;

            if (_networkManager.IsIP6Enabled)
            {
                values["HOST"] = "[ff01::2]:1900";
                message = BuildMessage(Header, values);
                task2 = _socketServer.SendMulticastMessageAsync(message, rootDevice.Address);
            }

            return Task.WhenAll(task1, task2);
        }

        private Task SendByeByeNotifications(SsdpDevice device, bool isRoot)
        {
            var tasks = new List<Task>();
            if (isRoot)
            {
                tasks.Add(SendByeByeNotification(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice)));
                if (this.SupportPnpRootDevice)
                {
                    tasks.Add(SendByeByeNotification(device, "pnp:rootdevice", GetUsn(device.Udn, "pnp:rootdevice")));
                }
            }

            tasks.Add(SendByeByeNotification(device, device.Udn, device.Udn));
            tasks.Add(SendByeByeNotification(
                device,
                string.Format(CultureInfo.InvariantCulture, "urn:{0}", device.FullDeviceType),
                GetUsn(device.Udn, device.FullDeviceType)));

            foreach (var childDevice in device.Devices)
            {
                tasks.Add(SendByeByeNotifications(childDevice, false));
            }

            return Task.WhenAll(tasks);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "byebye", Justification = "Correct value for this type of notification in SSDP.")]
        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            const string Header = "NOTIFY* HTTP/ 1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = "239.255.255.250:1900",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["SERVER"] = _server,
                ["NTS"] = "ssdp:byebye",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage(Header, values);

            var sendCount = IsDisposed ? 1 : 3;
            Task task1 = _socketServer.SendMulticastMessageAsync(message, sendCount, device.ToRootDevice().Address);
            Task task2 = Task.CompletedTask;

            if (_networkManager.IsIP6Enabled)
            {
                values["HOST"] = "[ff01::2]:1900";
                message = BuildMessage(Header, values);
                task2 = _socketServer.SendMulticastMessageAsync(message, sendCount, device.ToRootDevice().Address);
            }

            _logger.LogInformation("Sent byebye notification : {0}", device);

            return Task.WhenAll(task1, task2);
        }

        private void DisposeRebroadcastTimer()
        {
            var timer = _rebroadcastAliveNotificationsTimer;
            _rebroadcastAliveNotificationsTimer = null;
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        /// <summary>
        /// Expands the device's name.
        /// </summary>
        /// <param name="device">The device to expand.</param>
        private string Expand(SsdpDevice device)
        {
            if (device is SsdpRootDevice rootDevice)
            {
                return device.DeviceType + " - " + device.Uuid + " - " + rootDevice.Location;
            }
            else
            {
                return device.DeviceType + " - " + device.Uuid;
            }
        }

        private void RequestReceived(object sender, RequestReceivedEventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (string.Equals(e.Message.Method.Method, "M-SEARCH", StringComparison.OrdinalIgnoreCase))
            {
                // Only process requests that don't originate from ourselves.

                string agent = GetFirstHeaderValue("USER-AGENT", e.Message.Headers);
                if (string.Equals(agent, SsdpUserAgent + "\\" + _systemId, StringComparison.OrdinalIgnoreCase)
                    && _networkManager.IsValidInterfaceAddress(e.LocalIpAddress))
                {
                    _logger.LogDebug("Ignoring our own broadcast.");
                    return;
                }

                _ = ProcessSearchRequest(
                        GetFirstHeaderValue("MX", e.Message.Headers),
                        GetFirstHeaderValue("ST", e.Message.Headers),
                        e.ReceivedFrom,
                        e.LocalIpAddress);
            }
        }

        private class SearchRequest
        {
            public SearchRequest(EndPoint endPoint, string searchTarget, DateTime received)
            {
                EndPoint = endPoint;
                SearchTarget = searchTarget;
                Received = received;
            }

            /// <summary>
            /// Gets or sets the EndPoint.
            /// </summary>
            public EndPoint EndPoint { get; set; }

            /// <summary>
            /// Gets or sets the Received.
            /// </summary>
            public DateTime Received { get; set; }

            /// <summary>
            /// Gets or sets the SearchTarget.
            /// </summary>
            public string SearchTarget { get; set; }

            /// <summary>
            /// Gets the Key.
            /// </summary>
            public string Key
            {
                get { return this.SearchTarget + ":" + this.EndPoint.ToString(); }
            }

            public bool IsOld()
            {
                return DateTime.UtcNow.Subtract(this.Received).TotalMilliseconds > 500;
            }
        }
    }
}
