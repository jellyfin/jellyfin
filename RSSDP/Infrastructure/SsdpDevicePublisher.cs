using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    public class SsdpDevicePublisher : DisposableManagedObjectBase, ISsdpDevicePublisher
    {
        private const string ServerVersion = "1.0";

        private readonly INetworkManager _networkManager;

        private ISsdpCommunicationsServer _commsServer;
        private string _osName;
        private string _osVersion;
        private bool _sendOnlyMatchedHost;

        private bool _supportPnpRootDevice;

        private IList<SsdpRootDevice> _devices;
        private IReadOnlyList<SsdpRootDevice> _readOnlyDevices;

        private Timer _rebroadcastAliveNotificationsTimer;

        private IDictionary<string, SearchRequest> _recentSearchRequests;

        private Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpDevicePublisher"/> class.
        /// </summary>
        public SsdpDevicePublisher(
            ISsdpCommunicationsServer communicationsServer,
            INetworkManager networkManager,
            string osName,
            string osVersion,
            bool sendOnlyMatchedHost)
        {
            if (communicationsServer == null)
            {
                throw new ArgumentNullException(nameof(communicationsServer));
            }

            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
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

            _supportPnpRootDevice = true;
            _devices = new List<SsdpRootDevice>();
            _readOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_devices);
            _recentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _random = new Random();

            _networkManager = networkManager;
            _commsServer = communicationsServer;
            _commsServer.RequestReceived += CommsServer_RequestReceived;
            _osName = osName;
            _osVersion = osVersion;
            _sendOnlyMatchedHost = sendOnlyMatchedHost;

            _commsServer.BeginListeningForBroadcasts();
        }

        /// <summary>
        /// Gets a read only list of devices being published by this instance.
        /// </summary>
        public IEnumerable<SsdpRootDevice> Devices
        {
            get { return _readOnlyDevices; }
        }

        /// <summary>
        /// If true (default) treats root devices as both upnp:rootdevice and pnp:rootdevice types.
        /// </summary>
        /// <remarks>
        /// <para>Enabling this option will cause devices to show up in Microsoft Windows Explorer's network screens (if discovery is enabled etc.). Windows Explorer appears to search only for pnp:rootdeivce and not upnp:rootdevice.</para>
        /// <para>If false, the system will only use upnp:rootdevice for notifiation broadcasts and and search responses, which is correct according to the UPnP/SSDP spec.</para>
        /// </remarks>
        public bool SupportPnpRootDevice
        {
            get { return _supportPnpRootDevice; }
            set { _supportPnpRootDevice = value; }
        }

        public Action<string> LogFunction { get; set; }

        public void StartBroadcastingAliveMessages(TimeSpan interval)
        {
            _rebroadcastAliveNotificationsTimer = new Timer(SendAllAliveNotifications, null, TimeSpan.FromSeconds(5), interval);
        }

        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server, making them discoverable to SSDP clients.
        /// </summary>
        /// <remarks>
        /// <para>Adding a device causes "alive" notification messages to be sent immediately, or very soon after. Ensure your device/description service is running before adding the device object here.</para>
        /// <para>Devices added here with a non-zero cache life time will also have notifications broadcast periodically.</para>
        /// <para>This method ignores duplicate device adds (if the same device instance is added multiple times, the second and subsequent add calls do nothing).</para>
        /// </remarks>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <paramref name="device"/> contains property values that are not acceptable to the UPnP 1.0 specification.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "t", Justification = "Capture task to local variable supresses compiler warning, but task is not really needed.")]
        public void AddDevice(SsdpRootDevice device)
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
                WriteTrace("Device Added", device);

                SendAliveNotifications(device, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them undiscoverable.
        /// </summary>
        /// <remarks>
        /// <para>Removing a device causes "byebye" notification messages to be sent immediately, advising clients of the device/service becoming unavailable. We recommend removing the device from the published list before shutting down the actual device/service, if possible.</para>
        /// <para>This method does nothing if the device was not found in the collection.</para>
        /// </remarks>
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
                WriteTrace("Device Removed", device);

                await SendByeByeNotifications(device, true, CancellationToken.None).ConfigureAwait(false);
            }
        }

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

        private void ProcessSearchRequest(
            string mx,
            string searchTarget,
            IPEndPoint remoteEndPoint,
            IPAddress receivedOnlocalIpAddress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchTarget))
            {
                WriteTrace(string.Format(CultureInfo.InvariantCulture, "Invalid search request received From {0}, Target is null/empty.", remoteEndPoint));
                return;
            }

            // WriteTrace(string.Format("Search Request Received From {0}, Target = {1}", remoteEndPoint.ToString(), searchTarget));

            if (IsDuplicateSearchRequest(searchTarget, remoteEndPoint))
            {
                // WriteTrace("Search Request is Duplicate, ignoring.");
                return;
            }

            // Wait on random interval up to MX, as per SSDP spec.
            // Also, as per UPnP 1.1/SSDP spec ignore missing/bank MX header. If over 120, assume random value between 0 and 120.
            // Using 16 as minimum as that's often the minimum system clock frequency anyway.
            int maxWaitInterval = 0;
            if (string.IsNullOrEmpty(mx))
            {
                // Windows Explorer is poorly behaved and doesn't supply an MX header value.
                // if (this.SupportPnpRootDevice)
                mx = "1";
                // else
                // return;
            }

            if (!int.TryParse(mx, out maxWaitInterval) || maxWaitInterval <= 0)
            {
                return;
            }

            if (maxWaitInterval > 120)
            {
                maxWaitInterval = _random.Next(0, 120);
            }

            // Do not block synchronously as that may tie up a threadpool thread for several seconds.
            Task.Delay(_random.Next(16, maxWaitInterval * 1000)).ContinueWith((parentTask) =>
            {
                // Copying devices to local array here to avoid threading issues/enumerator exceptions.
                IEnumerable<SsdpDevice> devices = null;
                lock (_devices)
                {
                    if (string.Compare(SsdpConstants.SsdpDiscoverAllStHeader, searchTarget, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        devices = GetAllDevicesAsFlatEnumerable().ToArray();
                    }
                    else if (string.Compare(SsdpConstants.UpnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) == 0 || (this.SupportPnpRootDevice && string.Compare(SsdpConstants.PnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        devices = _devices.ToArray();
                    }
                    else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
                    {
                        devices = (from device in GetAllDevicesAsFlatEnumerable() where string.Compare(device.Uuid, searchTarget.Substring(5), StringComparison.OrdinalIgnoreCase) == 0 select device).ToArray();
                    }
                    else if (searchTarget.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
                    {
                        devices = (from device in GetAllDevicesAsFlatEnumerable() where string.Compare(device.FullDeviceType, searchTarget, StringComparison.OrdinalIgnoreCase) == 0 select device).ToArray();
                    }
                }

                if (devices != null)
                {
                    var deviceList = devices.ToList();
                    // WriteTrace(string.Format("Sending {0} search responses", deviceList.Count));

                    foreach (var device in deviceList)
                    {
                        if (!_sendOnlyMatchedHost ||
                            _networkManager.IsInSameSubnet(device.ToRootDevice().Address, remoteEndPoint.Address, device.ToRootDevice().SubnetMask))
                        {
                            SendDeviceSearchResponses(device, remoteEndPoint, receivedOnlocalIpAddress, cancellationToken);
                        }
                    }
                }
                else
                {
                    // WriteTrace(string.Format("Sending 0 search responses."));
                }
            }, TaskScheduler.Default);
        }

        private IEnumerable<SsdpDevice> GetAllDevicesAsFlatEnumerable()
        {
            return _devices.Union(_devices.SelectManyRecursive<SsdpDevice>((d) => d.Devices));
        }

        private void SendDeviceSearchResponses(
            SsdpDevice device,
            IPEndPoint endPoint,
            IPAddress receivedOnlocalIpAddress,
            CancellationToken cancellationToken)
        {
            bool isRootDevice = (device as SsdpRootDevice) != null;
            if (isRootDevice)
            {
                SendSearchResponse(SsdpConstants.UpnpDeviceTypeRootDevice, device, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice), endPoint, receivedOnlocalIpAddress, cancellationToken);
                if (this.SupportPnpRootDevice)
                {
                    SendSearchResponse(SsdpConstants.PnpDeviceTypeRootDevice, device, GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice), endPoint, receivedOnlocalIpAddress, cancellationToken);
                }
            }

            SendSearchResponse(device.Udn, device, device.Udn, endPoint, receivedOnlocalIpAddress, cancellationToken);

            SendSearchResponse(device.FullDeviceType, device, GetUsn(device.Udn, device.FullDeviceType), endPoint, receivedOnlocalIpAddress, cancellationToken);
        }

        private string GetUsn(string udn, string fullDeviceType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}::{1}", udn, fullDeviceType);
        }

        private async void SendSearchResponse(
            string searchTarget,
            SsdpDevice device,
            string uniqueServiceName,
            IPEndPoint endPoint,
            IPAddress receivedOnlocalIpAddress,
            CancellationToken cancellationToken)
        {
            var rootDevice = device.ToRootDevice();

            // var additionalheaders = FormatCustomHeadersForResponse(device);

            const string header = "HTTP/1.1 200 OK";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            values["EXT"] = string.Empty;
            values["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            values["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds;
            values["ST"] = searchTarget;
            values["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _osName, _osVersion, ServerVersion);
            values["USN"] = uniqueServiceName;
            values["LOCATION"] = rootDevice.Location.ToString();

            var message = BuildMessage(header, values);

            try
            {
                await _commsServer.SendMessage(
                        System.Text.Encoding.UTF8.GetBytes(message),
                        endPoint,
                        receivedOnlocalIpAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }

            // WriteTrace(string.Format("Sent search response to " + endPoint.ToString()), device);
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

        private void SendAllAliveNotifications(object state)
        {
            try
            {
                if (IsDisposed)
                {
                    return;
                }

                // WriteTrace("Begin Sending Alive Notifications For All Devices");

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

                    SendAliveNotifications(device, true, CancellationToken.None);
                }

                // WriteTrace("Completed Sending Alive Notifications For All Devices");
            }
            catch (ObjectDisposedException ex)
            {
                WriteTrace("Publisher stopped, exception " + ex.Message);
                Dispose();
            }
        }

        private void SendAliveNotifications(SsdpDevice device, bool isRoot, CancellationToken cancellationToken)
        {
            if (isRoot)
            {
                SendAliveNotification(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice), cancellationToken);
                if (this.SupportPnpRootDevice)
                {
                    SendAliveNotification(device, SsdpConstants.PnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice), cancellationToken);
                }
            }

            SendAliveNotification(device, device.Udn, device.Udn, cancellationToken);
            SendAliveNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType), cancellationToken);

            foreach (var childDevice in device.Devices)
            {
                SendAliveNotifications(childDevice, false, cancellationToken);
            }
        }

        private void SendAliveNotification(SsdpDevice device, string notificationType, string uniqueServiceName, CancellationToken cancellationToken)
        {
            var rootDevice = device.ToRootDevice();

            const string header = "NOTIFY * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // If needed later for non-server devices, these headers will need to be dynamic
            values["HOST"] = "239.255.255.250:1900";
            values["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            values["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds;
            values["LOCATION"] = rootDevice.Location.ToString();
            values["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _osName, _osVersion, ServerVersion);
            values["NTS"] = "ssdp:alive";
            values["NT"] = notificationType;
            values["USN"] = uniqueServiceName;

            var message = BuildMessage(header, values);

            _commsServer.SendMulticastMessage(message, _sendOnlyMatchedHost ? rootDevice.Address : null, cancellationToken);

            // WriteTrace(string.Format("Sent alive notification"), device);
        }

        private Task SendByeByeNotifications(SsdpDevice device, bool isRoot, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            if (isRoot)
            {
                tasks.Add(SendByeByeNotification(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice), cancellationToken));
                if (this.SupportPnpRootDevice)
                {
                    tasks.Add(SendByeByeNotification(device, "pnp:rootdevice", GetUsn(device.Udn, "pnp:rootdevice"), cancellationToken));
                }
            }

            tasks.Add(SendByeByeNotification(device, device.Udn, device.Udn, cancellationToken));
            tasks.Add(SendByeByeNotification(device, string.Format(CultureInfo.InvariantCulture, "urn:{0}", device.FullDeviceType), GetUsn(device.Udn, device.FullDeviceType), cancellationToken));

            foreach (var childDevice in device.Devices)
            {
                tasks.Add(SendByeByeNotifications(childDevice, false, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "byebye", Justification = "Correct value for this type of notification in SSDP.")]
        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName, CancellationToken cancellationToken)
        {
            const string header = "NOTIFY * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // If needed later for non-server devices, these headers will need to be dynamic
            values["HOST"] = "239.255.255.250:1900";
            values["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            values["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _osName, _osVersion, ServerVersion);
            values["NTS"] = "ssdp:byebye";
            values["NT"] = notificationType;
            values["USN"] = uniqueServiceName;

            var message = BuildMessage(header, values);

            var sendCount = IsDisposed ? 1 : 3;
            WriteTrace("Sent byebye notification", device);
            return _commsServer.SendMulticastMessage(message, sendCount, _sendOnlyMatchedHost ? device.ToRootDevice().Address : null, cancellationToken);
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
            IEnumerable<string> values;
            if (httpRequestHeaders.TryGetValues(headerName, out values) && values != null)
            {
                retVal = values.FirstOrDefault();
            }

            return retVal;
        }

        private void WriteTrace(string text)
        {
            if (LogFunction != null)
            {
                LogFunction(text);
            }

            // System.Diagnostics.Debug.WriteLine(text, "SSDP Publisher");
        }

        private void WriteTrace(string text, SsdpDevice device)
        {
            var rootDevice = device as SsdpRootDevice;
            if (rootDevice != null)
            {
                WriteTrace(text + " " + device.DeviceType + " - " + device.Uuid + " - " + rootDevice.Location);
            }
            else
            {
                WriteTrace(text + " " + device.DeviceType + " - " + device.Uuid);
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
                // According to SSDP/UPnP spec, ignore message if missing these headers.
                // Edit: But some devices do it anyway
                // if (!e.Message.Headers.Contains("MX"))
                //    WriteTrace("Ignoring search request - missing MX header.");
                // else if (!e.Message.Headers.Contains("MAN"))
                //    WriteTrace("Ignoring search request - missing MAN header.");
                // else
                ProcessSearchRequest(GetFirstHeaderValue(e.Message.Headers, "MX"), GetFirstHeaderValue(e.Message.Headers, "ST"), e.ReceivedFrom, e.LocalIpAddress, CancellationToken.None);
            }
        }

        private class SearchRequest
        {
            public IPEndPoint EndPoint { get; set; }

            public DateTime Received { get; set; }

            public string SearchTarget { get; set; }

            public string Key
            {
                get { return this.SearchTarget + ":" + this.EndPoint; }
            }

            public bool IsOld()
            {
                return DateTime.UtcNow.Subtract(this.Received).TotalMilliseconds > 500;
            }
        }
    }
}
