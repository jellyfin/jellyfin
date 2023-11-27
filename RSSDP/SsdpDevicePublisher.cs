using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    public class SsdpDevicePublisher : DisposableManagedObjectBase, ISsdpDevicePublisher
    {
        private ISsdpCommunicationsServer _CommsServer;
        private string _OSName;
        private string _OSVersion;
        private bool _sendOnlyMatchedHost;

        private bool _SupportPnpRootDevice;

        private IList<SsdpRootDevice> _Devices;
        private IReadOnlyList<SsdpRootDevice> _ReadOnlyDevices;

        private Timer _RebroadcastAliveNotificationsTimer;

        private IDictionary<string, SearchRequest> _RecentSearchRequests;

        private Random _Random;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SsdpDevicePublisher(
            ISsdpCommunicationsServer communicationsServer,
            string osName,
            string osVersion,
            bool sendOnlyMatchedHost)
        {
            ArgumentNullException.ThrowIfNull(communicationsServer);
            ArgumentNullException.ThrowIfNullOrEmpty(osName);
            ArgumentNullException.ThrowIfNullOrEmpty(osVersion);

            _SupportPnpRootDevice = true;
            _Devices = new List<SsdpRootDevice>();
            _ReadOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_Devices);
            _RecentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _Random = new Random();

            _CommsServer = communicationsServer;
            _CommsServer.RequestReceived += CommsServer_RequestReceived;
            _OSName = osName;
            _OSVersion = osVersion;
            _sendOnlyMatchedHost = sendOnlyMatchedHost;

            _CommsServer.BeginListeningForMulticast();

            // Send alive notification once on creation
            SendAllAliveNotifications(null);
        }

        public void StartSendingAliveNotifications(TimeSpan interval)
        {
            _RebroadcastAliveNotificationsTimer = new Timer(SendAllAliveNotifications, null, TimeSpan.FromSeconds(5), interval);
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
        public void AddDevice(SsdpRootDevice device)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            ThrowIfDisposed();

            bool wasAdded = false;
            lock (_Devices)
            {
                if (!_Devices.Contains(device))
                {
                    _Devices.Add(device);
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
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            bool wasRemoved = false;
            lock (_Devices)
            {
                if (_Devices.Contains(device))
                {
                    _Devices.Remove(device);
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
        /// Returns a read only list of devices being published by this instance.
        /// </summary>
        public IEnumerable<SsdpRootDevice> Devices
        {
            get
            {
                return _ReadOnlyDevices;
            }
        }

        /// <summary>
        /// If true (default) treats root devices as both upnp:rootdevice and pnp:rootdevice types.
        /// </summary>
        /// <remarks>
        /// <para>Enabling this option will cause devices to show up in Microsoft Windows Explorer's network screens (if discovery is enabled etc.). Windows Explorer appears to search only for pnp:rootdeivce and not upnp:rootdevice.</para>
        /// <para>If false, the system will only use upnp:rootdevice for notification broadcasts and and search responses, which is correct according to the UPnP/SSDP spec.</para>
        /// </remarks>
        public bool SupportPnpRootDevice
        {
            get { return _SupportPnpRootDevice; }

            set
            {
                _SupportPnpRootDevice = value;
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

                var commsServer = _CommsServer;
                if (commsServer is not null)
                {
                    commsServer.RequestReceived -= this.CommsServer_RequestReceived;
                }

                var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                Task.WaitAll(tasks);

                _CommsServer = null;
                if (commsServer is not null)
                {
                    if (!commsServer.IsShared)
                    {
                        commsServer.Dispose();
                    }
                }

                _RecentSearchRequests = null;
            }
        }

        private void ProcessSearchRequest(
            string mx,
            string searchTarget,
            IPEndPoint remoteEndPoint,
            IPAddress receivedOnlocalIPAddress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchTarget))
            {
                WriteTrace(string.Format(CultureInfo.InvariantCulture, "Invalid search request received From {0}, Target is null/empty.", remoteEndPoint.ToString()));
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

            if (!int.TryParse(mx, out var maxWaitInterval) || maxWaitInterval <= 0)
            {
                return;
            }

            if (maxWaitInterval > 120)
            {
                maxWaitInterval = _Random.Next(0, 120);
            }

            // Do not block synchronously as that may tie up a threadpool thread for several seconds.
            Task.Delay(_Random.Next(16, maxWaitInterval * 1000), cancellationToken).ContinueWith((parentTask) =>
            {
                // Copying devices to local array here to avoid threading issues/enumerator exceptions.
                IEnumerable<SsdpDevice> devices = null;
                lock (_Devices)
                {
                    if (string.Compare(SsdpConstants.SsdpDiscoverAllSTHeader, searchTarget, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        devices = GetAllDevicesAsFlatEnumerable().ToArray();
                    }
                    else if (string.Compare(SsdpConstants.UpnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) == 0 || (SupportPnpRootDevice && String.Compare(SsdpConstants.PnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        devices = _Devices.ToArray();
                    }
                    else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
                    {
                        devices = GetAllDevicesAsFlatEnumerable().Where(d => string.Compare(d.Uuid, searchTarget.Substring(5), StringComparison.OrdinalIgnoreCase) == 0).ToArray();
                    }
                    else if (searchTarget.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
                    {
                        devices = GetAllDevicesAsFlatEnumerable().Where(d => string.Compare(d.FullDeviceType, searchTarget, StringComparison.OrdinalIgnoreCase) == 0).ToArray();
                    }
                }

                if (devices is not null)
                {
                    // WriteTrace(String.Format("Sending {0} search responses", deviceList.Count));

                    foreach (var device in devices)
                    {
                        var root = device.ToRootDevice();

                        if (!_sendOnlyMatchedHost || root.Address.Equals(receivedOnlocalIPAddress))
                        {
                            SendDeviceSearchResponses(device, remoteEndPoint, receivedOnlocalIPAddress, cancellationToken);
                        }
                    }
                }
            }, cancellationToken);
        }

        private IEnumerable<SsdpDevice> GetAllDevicesAsFlatEnumerable()
        {
            return _Devices.Union(_Devices.SelectManyRecursive<SsdpDevice>((d) => d.Devices));
        }

        private void SendDeviceSearchResponses(
            SsdpDevice device,
            IPEndPoint endPoint,
            IPAddress receivedOnlocalIPAddress,
            CancellationToken cancellationToken)
        {
            bool isRootDevice = (device as SsdpRootDevice) is not null;
            if (isRootDevice)
            {
                SendSearchResponse(SsdpConstants.UpnpDeviceTypeRootDevice, device, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice), endPoint, receivedOnlocalIPAddress, cancellationToken);
                if (SupportPnpRootDevice)
                {
                    SendSearchResponse(SsdpConstants.PnpDeviceTypeRootDevice, device, GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice), endPoint, receivedOnlocalIPAddress, cancellationToken);
                }
            }

            SendSearchResponse(device.Udn, device, device.Udn, endPoint, receivedOnlocalIPAddress, cancellationToken);

            SendSearchResponse(device.FullDeviceType, device, GetUsn(device.Udn, device.FullDeviceType), endPoint, receivedOnlocalIPAddress, cancellationToken);
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
            IPAddress receivedOnlocalIPAddress,
            CancellationToken cancellationToken)
        {
            const string header = "HTTP/1.1 200 OK";

            var rootDevice = device.ToRootDevice();
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = "",
                ["DATE"] = DateTime.UtcNow.ToString("r"),
                ["HOST"] = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", SsdpConstants.MulticastLocalAdminAddress, SsdpConstants.MulticastPort),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _OSName, _OSVersion, SsdpConstants.ServerVersion),
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location.ToString()
            };

            var message = BuildMessage(header, values);

            try
            {
                await _CommsServer.SendMessage(
                        Encoding.UTF8.GetBytes(message),
                        endPoint,
                        receivedOnlocalIPAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
            }

            // WriteTrace(String.Format("Sent search response to " + endPoint.ToString()), device);
        }

        private bool IsDuplicateSearchRequest(string searchTarget, IPEndPoint endPoint)
        {
            var isDuplicateRequest = false;

            var newRequest = new SearchRequest() { EndPoint = endPoint, SearchTarget = searchTarget, Received = DateTime.UtcNow };
            lock (_RecentSearchRequests)
            {
                if (_RecentSearchRequests.ContainsKey(newRequest.Key))
                {
                    var lastRequest = _RecentSearchRequests[newRequest.Key];
                    if (lastRequest.IsOld())
                    {
                        _RecentSearchRequests[newRequest.Key] = newRequest;
                    }
                    else
                    {
                        isDuplicateRequest = true;
                    }
                }
                else
                {
                    _RecentSearchRequests.Add(newRequest.Key, newRequest);
                    if (_RecentSearchRequests.Count > 10)
                    {
                        CleanUpRecentSearchRequestsAsync();
                    }
                }
            }

            return isDuplicateRequest;
        }

        private void CleanUpRecentSearchRequestsAsync()
        {
            lock (_RecentSearchRequests)
            {
                foreach (var requestKey in (from r in _RecentSearchRequests where r.Value.IsOld() select r.Key).ToArray())
                {
                    _RecentSearchRequests.Remove(requestKey);
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
                lock (_Devices)
                {
                    devices = _Devices.ToArray();
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
                if (SupportPnpRootDevice)
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

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", SsdpConstants.MulticastLocalAdminAddress, SsdpConstants.MulticastPort),
                ["DATE"] = DateTime.UtcNow.ToString("r"),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["LOCATION"] = rootDevice.Location.ToString(),
                ["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _OSName, _OSVersion, SsdpConstants.ServerVersion),
                ["NTS"] = "ssdp:alive",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage(header, values);

            _CommsServer.SendMulticastMessage(message, _sendOnlyMatchedHost ? rootDevice.Address : null, cancellationToken);

            // WriteTrace(String.Format("Sent alive notification"), device);
        }

        private Task SendByeByeNotifications(SsdpDevice device, bool isRoot, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            if (isRoot)
            {
                tasks.Add(SendByeByeNotification(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice), cancellationToken));
                if (SupportPnpRootDevice)
                {
                    tasks.Add(SendByeByeNotification(device, "pnp:rootdevice", GetUsn(device.Udn, "pnp:rootdevice"), cancellationToken));
                }
            }

            tasks.Add(SendByeByeNotification(device, device.Udn, device.Udn, cancellationToken));
            tasks.Add(SendByeByeNotification(device, String.Format(CultureInfo.InvariantCulture, "urn:{0}", device.FullDeviceType), GetUsn(device.Udn, device.FullDeviceType), cancellationToken));

            foreach (var childDevice in device.Devices)
            {
                tasks.Add(SendByeByeNotifications(childDevice, false, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName, CancellationToken cancellationToken)
        {
            const string header = "NOTIFY * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", SsdpConstants.MulticastLocalAdminAddress, SsdpConstants.MulticastPort),
                ["DATE"] = DateTime.UtcNow.ToString("r"),
                ["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _OSName, _OSVersion, SsdpConstants.ServerVersion),
                ["NTS"] = "ssdp:byebye",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage(header, values);

            var sendCount = IsDisposed ? 1 : 3;
            WriteTrace(string.Format(CultureInfo.InvariantCulture, "Sent byebye notification"), device);
            return _CommsServer.SendMulticastMessage(message, sendCount, _sendOnlyMatchedHost ? device.ToRootDevice().Address : null, cancellationToken);
        }

        private void DisposeRebroadcastTimer()
        {
            var timer = _RebroadcastAliveNotificationsTimer;
            _RebroadcastAliveNotificationsTimer = null;
            timer?.Dispose();
        }

        private TimeSpan GetMinimumNonZeroCacheLifetime()
        {
            var nonzeroCacheLifetimesQuery = (
                from device
                in _Devices
                where device.CacheLifetime != TimeSpan.Zero
                select device.CacheLifetime).ToList();

            if (nonzeroCacheLifetimesQuery.Any())
            {
                return nonzeroCacheLifetimesQuery.Min();
            }

            return TimeSpan.Zero;
        }

        private string GetFirstHeaderValue(System.Net.Http.Headers.HttpRequestHeaders httpRequestHeaders, string headerName)
        {
            string retVal = null;
            if (httpRequestHeaders.TryGetValues(headerName, out var values) && values is not null)
            {
                retVal = values.FirstOrDefault();
            }

            return retVal;
        }

        public Action<string> LogFunction { get; set; }

        private void WriteTrace(string text)
        {
            LogFunction?.Invoke(text);
            // System.Diagnostics.Debug.WriteLine(text, "SSDP Publisher");
        }

        private void WriteTrace(string text, SsdpDevice device)
        {
            var rootDevice = device as SsdpRootDevice;
            if (rootDevice is not null)
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
                ProcessSearchRequest(GetFirstHeaderValue(e.Message.Headers, "MX"), GetFirstHeaderValue(e.Message.Headers, "ST"), e.ReceivedFrom, e.LocalIPAddress, CancellationToken.None);
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
