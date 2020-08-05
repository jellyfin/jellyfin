using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    public class SsdpDevicePublisher : DisposableManagedObjectBase, ISsdpDevicePublisher
    {
        private readonly ILogger _logger;
        private readonly string _server;
        private readonly string _systemId;
        private readonly bool _sendOnlyMatchedHost;
        private readonly IList<SsdpRootDevice> _devices;
        private readonly IReadOnlyList<SsdpRootDevice> _readOnlyDevices;
        private readonly INetworkManager _networkManager;
        private readonly Random _random;
        private ISsdpCommunicationsServer _commsServer;
        private Timer _rebroadcastAliveNotificationsTimer;
        private IDictionary<string, SearchRequest> _recentSearchRequests;
        
        public SsdpDevicePublisher(
            ISsdpCommunicationsServer communicationsServer,
            string osName,
            string osVersion,
            string systemId,
            ILogger logger,
            INetworkManager networkManager,
            bool sendOnlyMatchedHost)
        {
            if (communicationsServer == null)
            {
                throw new ArgumentNullException(nameof(communicationsServer));
            }

            if (osName == null)
            {
                throw new ArgumentNullException(nameof(osName));
            }

            if (osName.Length == 0)
            {
                throw new ArgumentException("osName cannot be an empty string.", nameof(osName));
            }

            if (osVersion == null)
            {
                throw new ArgumentNullException(nameof(osVersion));
            }

            if (osVersion.Length == 0)
            {
                throw new ArgumentException("osVersion cannot be an empty string.", nameof(osName));
            }

            SupportPnpRootDevice = true;
            _devices = new List<SsdpRootDevice>();
            _readOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_devices);
            _recentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _random = new Random();
            _logger = logger;
            _commsServer = communicationsServer ?? throw new ArgumentNullException(nameof(communicationsServer));
            _commsServer.RequestReceived += CommsServer_RequestReceived;
            _sendOnlyMatchedHost = sendOnlyMatchedHost;
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _commsServer.BeginListeningForBroadcasts();
            _server = $"{osName}/{osVersion} UPnP/1.0 RSSDP/1.0";
            _systemId = systemId;
        }

        public void StartBroadcastingAliveMessages(TimeSpan interval)
        {
            if (_rebroadcastAliveNotificationsTimer != null)
            {
                _rebroadcastAliveNotificationsTimer.Change(TimeSpan.FromSeconds(5), interval);
            }
            else
            {
                _rebroadcastAliveNotificationsTimer = new Timer(delegate
                {
                    SendAllAliveNotificationsAsync();
                },
                null,
                TimeSpan.FromSeconds(5),
                interval);
            }
        }

        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server, making them discoverable to SSDP clients.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "t", Justification = "Capture task to local variable supresses compiler warning, but task is not really needed.")]
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
                _logger.LogInformation("Device Added {0}", Expand(device));

                await SendAliveNotifications(device, true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them undiscoverable.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
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
        /// Returns a read only list of devices being published by this instance.
        /// </summary>
        public IEnumerable<SsdpRootDevice> Devices
        {
            get
            {
                return _readOnlyDevices;
            }
        }

        /// <summary>
        /// If true (default) treats root devices as both upnp:rootdevice and pnp:rootdevice types.
        /// </summary>
        public bool SupportPnpRootDevice { get; set; }

        /// <summary>
        /// Stops listening for requests, stops sending periodic broadcasts, disposes all internal resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRebroadcastTimer();

                var commsServer = _commsServer;
                if (commsServer != null)
                {
                    commsServer.RequestReceived -= this.CommsServer_RequestReceived;
                }

                var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                Task.WaitAll(tasks);

                _commsServer = null;
                if (commsServer != null)
                {
                    if (!commsServer.IsShared)
                    {
                        commsServer.Dispose();
                    }
                }

                _recentSearchRequests = null;
            }
        }

        private async Task ProcessSearchRequest(
            string mx,
            string searchTarget,
            IPEndPoint remoteEndPoint,
            IPAddress receivedOnlocalIpAddress)
        {
            if (String.IsNullOrEmpty(searchTarget))
            {
                _logger.LogWarning("Invalid search request received From {0}, Target is null/empty.", remoteEndPoint);
                return;
            }

            // WriteTrace(String.Format("Search Request Received From {0}, Target = {1}", remoteEndPoint.ToString(), searchTarget));

            if (IsDuplicateSearchRequest(searchTarget, remoteEndPoint))
            {
                // WriteTrace("Search Request is Duplicate, ignoring.");
                return;
            }

            // Wait on random interval up to MX, as per SSDP spec.
            // Also, as per UPnP 1.1/SSDP spec ignore missing/bank MX header. If over 120, assume random value between 0 and 120.
            // Using 16 as minimum as that's often the minimum system clock frequency anyway.
            if (String.IsNullOrEmpty(mx))
            {
                // Windows Explorer is poorly behaved and doesn't supply an MX header value.
                // if (this.SupportPnpRootDevice)
                mx = "1";
                // else
                // return;
            }

            if (!Int32.TryParse(mx, out int maxWaitInterval) || maxWaitInterval <= 0)
            {
                return;
            }

            if (maxWaitInterval > 120)
            {
                maxWaitInterval = _random.Next(0, 120);
            }

            // Do not block synchronously as that may tie up a threadpool thread for several seconds.

            _ = Task.Delay(_random.Next(16, (maxWaitInterval * 1000)));

            await ProcessSearchRequestAsync(searchTarget, remoteEndPoint, receivedOnlocalIpAddress).ConfigureAwait(false);
        }

        private async Task ProcessSearchRequestAsync(
            string searchTarget,
            IPEndPoint remoteEndPoint,
            IPAddress receivedOnlocalIpAddress)
        {
            // Copying devices to local array here to avoid threading issues/enumerator exceptions.
            IEnumerable<SsdpDevice> devices = null;
            lock (_devices)
            {
                if (string.Equals(SsdpConstants.SsdpDiscoverAllSTHeader, searchTarget, StringComparison.OrdinalIgnoreCase))
                {
                    devices = GetAllDevicesAsFlatEnumerable().ToArray();
                }
                else if (string.Equals(SsdpConstants.UpnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) ||
                    (this.SupportPnpRootDevice && string.Equals(SsdpConstants.PnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase)))
                {
                    devices = _devices.ToArray();
                }
                else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
                {
                    devices = (from device in GetAllDevicesAsFlatEnumerable()
                                where string.Equals(device.Uuid, searchTarget.Substring(5), StringComparison.OrdinalIgnoreCase)
                                select device).ToArray();
                }
                else if (searchTarget.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
                {
                    devices = (from device in GetAllDevicesAsFlatEnumerable()
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

                    // TODO: Check to see when _sendOnlyMatchedHost would be of use!

                    // Response only two machines in the same subnet, or the are private address in the local machine.
                    // (eg when something else is running on this machine with 127.0.0.1 and interface address).
                    if (!_sendOnlyMatchedHost ||
                        _networkManager.IsInSameSubnet(rt.Address, rt.SubnetMask, remoteEndPoint.Address) ||
                        _networkManager.OnSameMachine(rt.Address, remoteEndPoint.Address))

                    {
                        await SendDeviceSearchResponsesAsync(device, remoteEndPoint, receivedOnlocalIpAddress).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                //_logger.LogInformation("Sending 0 search responses.");
            }
        }

        private IEnumerable<SsdpDevice> GetAllDevicesAsFlatEnumerable()
        {
            return _devices.Union(_devices.SelectManyRecursive<SsdpDevice>((d) => d.Devices));
        }

        private async Task SendDeviceSearchResponsesAsync(
            SsdpDevice device,
            IPEndPoint endPoint,
            IPAddress receivedOnlocalIpAddress)
        {
            bool isRootDevice = (device as SsdpRootDevice) != null;
            if (isRootDevice)
            {
                await SendSearchResponseAsync(
                    SsdpConstants.UpnpDeviceTypeRootDevice,
                    device,
                    GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice),
                    endPoint,
                    receivedOnlocalIpAddress).ConfigureAwait(false);

                if (this.SupportPnpRootDevice)
                {
                    await SendSearchResponseAsync(
                        SsdpConstants.PnpDeviceTypeRootDevice,
                        device,
                        GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice),
                        endPoint,
                        receivedOnlocalIpAddress).ConfigureAwait(false); ;
                }
            }

            await SendSearchResponseAsync(
                device.Udn,
                device,
                device.Udn,
                endPoint,
                receivedOnlocalIpAddress).ConfigureAwait(false);

            await SendSearchResponseAsync(
                device.FullDeviceType,
                device,
                GetUsn(device.Udn, device.FullDeviceType),
                endPoint,
                receivedOnlocalIpAddress).ConfigureAwait(false);
        }

        private static string GetUsn(string udn, string fullDeviceType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}::{1}", udn, fullDeviceType);
        }

        private async Task SendSearchResponseAsync(
            string searchTarget,
            SsdpDevice device,
            string uniqueServiceName,
            IPEndPoint endPoint,
            IPAddress receivedOnlocalIpAddress)
        {
            var rootDevice = device.ToRootDevice();

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = "",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["SERVER"] = _server,
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location.ToString()
            };

            var message = BuildMessage("HTTP/1.1 200 OK", values);

            try
            {
                await _commsServer.SendMessage(System.Text.Encoding.UTF8.GetBytes(message), endPoint, receivedOnlocalIpAddress).ConfigureAwait(false);
                _logger.LogInformation("Sent search response to {0} : {1}", endPoint, device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessage failed.");
            }
        }

        private bool IsDuplicateSearchRequest(string searchTarget, IPEndPoint endPoint)
        {
            var isDuplicateRequest = false;

            var newRequest = new SearchRequest() { EndPoint = endPoint, SearchTarget = searchTarget, Received = DateTime.UtcNow };
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
                await SendAliveNotificationAsync(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice)).ConfigureAwait(false);
                if (this.SupportPnpRootDevice)
                {
                    await SendAliveNotificationAsync(device, SsdpConstants.PnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice)).ConfigureAwait(false);
                }
            }

            await SendAliveNotificationAsync(device, device.Udn, device.Udn).ConfigureAwait(false);
            await SendAliveNotificationAsync(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Devices)
            {
                await SendAliveNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        private async Task SendAliveNotificationAsync(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
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

            var message = BuildMessage("NOTIFY * HTTP / 1.1", values);

            await _commsServer.SendMulticastMessage(message, _sendOnlyMatchedHost ? rootDevice.Address : null).ConfigureAwait(false);

            // _logger.LogInformation("Transmitted alive notification : {0} - {1}", device.FriendlyName, device);
        }

        private Task SendByeByeNotifications(SsdpDevice device, bool isRoot)
        {
            var tasks = new List<Task>();
            if (isRoot)
            {
                tasks.Add(SendByeByeNotification(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice)));
                if (this.SupportPnpRootDevice)
                {
                    tasks.Add(SendByeByeNotification(device, "pnp:rootdevice", GetUsn(device.Udn, "pnp:rootdevice")));
                }
            }

            tasks.Add(SendByeByeNotification(device, device.Udn, device.Udn));
            tasks.Add(SendByeByeNotification(
                device,
                String.Format(CultureInfo.InvariantCulture, "urn:{0}", device.FullDeviceType),
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
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = device.ToRootDevice().Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ?
                    $"{SsdpConstants.MulticastLocalAdminAddress}:{SsdpConstants.MulticastPort}" :
                    $"{SsdpConstants.MulticastLocalAdminAddressV6}:{SsdpConstants.MulticastPort}",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["SERVER"] = _server,
                ["NTS"] = SsdpConstants.SsdpByeByeNotification,
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage("NOTIFY * HTTP/1.1", values);

            var sendCount = IsDisposed ? 1 : 3;
            _logger.LogInformation("Sent byebye notification : {0}", device);
            return _commsServer.SendMulticastMessage(message, sendCount, _sendOnlyMatchedHost ? device.ToRootDevice().Address : null);
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

        private string GetFirstHeaderValue(System.Net.Http.Headers.HttpRequestHeaders httpRequestHeaders, string headerName)
        {
            string retVal = null;
            if (httpRequestHeaders.TryGetValues(headerName, out IEnumerable<String> values) && values != null)
            {
                retVal = values.FirstOrDefault();
            }

            return retVal;
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

        private void CommsServer_RequestReceived(object sender, RequestReceivedEventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (string.Equals(e.Message.Method.Method, SsdpConstants.MSearchMethod, StringComparison.OrdinalIgnoreCase))
            {
                // Only process requests that don't originate from ourselves.

                string agent = GetFirstHeaderValue(e.Message.Headers, "USER-AGENT");
                if (string.Equals(agent, SsdpConstants.SsdpUserAgent + "\\" + _systemId, StringComparison.OrdinalIgnoreCase)
                    && _networkManager.IsValidInterfaceAddress(e.LocalIpAddress))
                {
                    _logger.LogDebug("Ignoring our own broadcast.");
                    return;
                }

                _ = ProcessSearchRequest(
                        GetFirstHeaderValue(e.Message.Headers, "MX"),
                        GetFirstHeaderValue(e.Message.Headers, "ST"),
                        e.ReceivedFrom,
                        e.LocalIpAddress);
            }
        }

        private class SearchRequest
        {
            /// <summary>
            /// Gets or sets the EndPoint.
            /// </summary>
            public IPEndPoint EndPoint { get; set; }

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
