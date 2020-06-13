namespace Rssdp.Infrastructure
{
    using System;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Networking;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    public class SsdpDevicePublisher : DisposableManagedObjectBase, ISsdpDevicePublisher
    {
        /// <summary>
        /// Defines the _CommsServer.
        /// </summary>
        private ISsdpCommunicationsServer _CommsServer;

        private ILogger _logger;

        /// <summary>
        /// Defines the _OSName.
        /// </summary>
        private string _OSName;

        /// <summary>
        /// Defines the _OSVersion.
        /// </summary>
        private string _OSVersion;

        /// <summary>
        /// Defines the _sendOnlyMatchedHost.
        /// </summary>
        private bool _sendOnlyMatchedHost;

        /// <summary>
        /// Defines the _SupportPnpRootDevice.
        /// </summary>
        private bool _SupportPnpRootDevice;

        /// <summary>
        /// Defines the _Devices.
        /// </summary>
        private IList<SsdpRootDevice> _Devices;

        /// <summary>
        /// Defines the _ReadOnlyDevices.
        /// </summary>
        private IReadOnlyList<SsdpRootDevice> _ReadOnlyDevices;

        /// <summary>
        /// Defines the _RebroadcastAliveNotificationsTimer.
        /// </summary>
        private Timer _RebroadcastAliveNotificationsTimer;

        /// <summary>
        /// Defines the _RecentSearchRequests.
        /// </summary>
        private IDictionary<string, SearchRequest> _RecentSearchRequests;

        /// <summary>
        /// Defines the _Random.
        /// </summary>
        private Random _Random;

        /// <summary>
        /// Defines the ServerVersion.
        /// </summary>
        private const string ServerVersion = "1.0";

        private INetworkManager _networkManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpDevicePublisher"/> class.
        /// </summary>
        /// <param name="communicationsServer">The communicationsServer<see cref="ISsdpCommunicationsServer"/>.</param>
        /// <param name="osName">The osName<see cref="string"/>.</param>
        /// <param name="osVersion">The osVersion<see cref="string"/>.</param>
        /// <param name="sendOnlyMatchedHost">The sendOnlyMatchedHost<see cref="bool"/>.</param>
        public SsdpDevicePublisher(ISsdpCommunicationsServer communicationsServer, string osName, string osVersion, ILogger logger, INetworkManager networkManager, bool sendOnlyMatchedHost)
        {
            if (osName == null) throw new ArgumentNullException(nameof(osName));
            if (osName.Length == 0) throw new ArgumentException("osName cannot be an empty string.", nameof(osName));
            if (osVersion == null) throw new ArgumentNullException(nameof(osVersion));
            if (osVersion.Length == 0) throw new ArgumentException("osVersion cannot be an empty string.", nameof(osName));

            _SupportPnpRootDevice = true;
            _Devices = new List<SsdpRootDevice>();
            _ReadOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_Devices);
            _RecentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _Random = new Random();
            _logger = logger;
            _CommsServer = communicationsServer ?? throw new ArgumentNullException(nameof(communicationsServer));
            _CommsServer.RequestReceived += CommsServer_RequestReceived;
            _OSName = osName;
            _OSVersion = osVersion;
            _sendOnlyMatchedHost = sendOnlyMatchedHost;
            _networkManager = networkManager;
            _CommsServer.BeginListeningForBroadcasts();
        }

        /// <summary>
        /// The SetBroadcastingAliveMessages.
        /// </summary>
        /// <param name="interval">The interval<see cref="TimeSpan"/>.</param>
        public void SetBroadcastingAliveMessages(TimeSpan interval)
        {
            if (_RebroadcastAliveNotificationsTimer != null)
            {
                _RebroadcastAliveNotificationsTimer.Change(TimeSpan.FromSeconds(5), interval);
            }
            else
            {
                _RebroadcastAliveNotificationsTimer = new Timer(SendAllAliveNotifications, null, TimeSpan.FromSeconds(5), interval);
            }
        }

        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server, making them discoverable to SSDP clients.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "t", Justification = "Capture task to local variable supresses compiler warning, but task is not really needed.")]
        public void AddDevice(SsdpRootDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

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
                _logger.LogInformation("Device Added {0}", Expand(device));

                SendAliveNotifications(device, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them undiscoverable.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task RemoveDevice(SsdpRootDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

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
                _logger.LogInformation("Device Removed {0}", Expand(device));

                await SendByeByeNotifications(device, true, CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the Devices
        /// Returns a read only list of devices being published by this instance..
        /// </summary>
        public IEnumerable<SsdpRootDevice> Devices
        {
            get
            {
                return _ReadOnlyDevices;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether SupportPnpRootDevice
        /// If true (default) treats root devices as both upnp:rootdevice and pnp:rootdevice types..
        /// </summary>
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
        /// <param name="disposing">.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRebroadcastTimer();

                var commsServer = _CommsServer;
                if (commsServer != null)
                {
                    commsServer.RequestReceived -= this.CommsServer_RequestReceived;
                }

                var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                Task.WaitAll(tasks);

                _CommsServer = null;
                if (commsServer != null)
                {
                    if (!commsServer.IsShared)
                        commsServer.Dispose();
                }

                _RecentSearchRequests = null;
            }
        }

        /// <summary>
        /// Process a response from a SSDP search.
        /// </summary>
        /// <param name="mx">The mx<see cref="string"/>.</param>
        /// <param name="searchTarget">The searchTarget<see cref="string"/>.</param>
        /// <param name="remoteEndPoint">The remoteEndPoint<see cref="IPEndPoint"/>.</param>
        /// <param name="receivedOnlocalIpAddress">The receivedOnlocalIpAddress<see cref="IPAddress"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        private void ProcessSearchRequest(
            string mx,
            string searchTarget,
            IPEndPoint remoteEndPoint,
            IPAddress receivedOnlocalIpAddress,
            CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(searchTarget))
            {
                _logger.LogWarning("Invalid search request received From {0}, Target is null/empty.", remoteEndPoint);
                return;
            }

            //WriteTrace(String.Format("Search Request Received From {0}, Target = {1}", remoteEndPoint.ToString(), searchTarget));

            if (IsDuplicateSearchRequest(searchTarget, remoteEndPoint))
            {
                //WriteTrace("Search Request is Duplicate, ignoring.");
                return;
            }


            if (_networkManager.IsValidInterfaceAddress(remoteEndPoint.Address))
            {
                // Processing our own message!
                return; 
            }

            //Wait on random interval up to MX, as per SSDP spec.
            //Also, as per UPnP 1.1/SSDP spec ignore missing/bank MX header. If over 120, assume random value between 0 and 120.
            //Using 16 as minimum as that's often the minimum system clock frequency anyway.
            int maxWaitInterval = 0;
            if (String.IsNullOrEmpty(mx))
            {
                //Windows Explorer is poorly behaved and doesn't supply an MX header value.
                //if (this.SupportPnpRootDevice)
                mx = "1";
                //else
                //return;
            }

            if (!Int32.TryParse(mx, out maxWaitInterval) || maxWaitInterval <= 0) return;

            if (maxWaitInterval > 120)
                maxWaitInterval = _Random.Next(0, 120);

            //Do not block synchronously as that may tie up a threadpool thread for several seconds.
            Task.Delay(_Random.Next(16, maxWaitInterval * 1000))
                .ContinueWith((parentTask) =>
              {
                  //Copying devices to local array here to avoid threading issues/enumerator exceptions.
                  IEnumerable<SsdpDevice> devices = null;
                  lock (_Devices)
                  {
                      if (String.Compare(SsdpConstants.SsdpDiscoverAllSTHeader, searchTarget, StringComparison.OrdinalIgnoreCase) == 0)
                          devices = GetAllDevicesAsFlatEnumerable().ToArray();
                      else if (String.Compare(SsdpConstants.UpnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) == 0 || (this.SupportPnpRootDevice && String.Compare(SsdpConstants.PnpDeviceTypeRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase) == 0))
                          devices = _Devices.ToArray();
                      else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
                          devices = (from device in GetAllDevicesAsFlatEnumerable() where string.Compare(device.Uuid, searchTarget.Substring(5), StringComparison.OrdinalIgnoreCase) == 0 select device).ToArray();
                      else if (searchTarget.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
                          devices = (from device in GetAllDevicesAsFlatEnumerable() where string.Compare(device.FullDeviceType, searchTarget, StringComparison.OrdinalIgnoreCase) == 0 select device).ToArray();
                  }

                  if (devices != null)
                  {
                      var deviceList = devices.ToList();

                      foreach (var device in deviceList)
                      {
                          var rt = device.ToRootDevice();
                                                    
                          if (!_sendOnlyMatchedHost ||
                              NetworkManager.IsInSameSubnet(rt.Address, rt.SubnetMask, remoteEndPoint.Address))
                          {
                              _logger.LogInformation("Sending response to {0} {1}.", rt.Address, rt.ModelName);
                              SendDeviceSearchResponses(device, remoteEndPoint, receivedOnlocalIpAddress, cancellationToken);
                          }
                      }
                  }
                  else
                  {
                      //_logger.LogInformation("Sending 0 search responses.");
                  }
              });
        }

        /// <summary>
        /// The GetAllDevicesAsFlatEnumerable.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{SsdpDevice}"/>.</returns>
        private IEnumerable<SsdpDevice> GetAllDevicesAsFlatEnumerable()
        {
            return _Devices.Union(_Devices.SelectManyRecursive<SsdpDevice>((d) => d.Devices));
        }

        /// <summary>
        /// The SendDeviceSearchResponses.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="endPoint">The endPoint<see cref="IPEndPoint"/>.</param>
        /// <param name="receivedOnlocalIpAddress">The receivedOnlocalIpAddress<see cref="IPAddress"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
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
                    SendSearchResponse(SsdpConstants.PnpDeviceTypeRootDevice, device, GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice), endPoint, receivedOnlocalIpAddress, cancellationToken);
            }

            SendSearchResponse(device.Udn, device, device.Udn, endPoint, receivedOnlocalIpAddress, cancellationToken);

            SendSearchResponse(device.FullDeviceType, device, GetUsn(device.Udn, device.FullDeviceType), endPoint, receivedOnlocalIpAddress, cancellationToken);
        }

        /// <summary>
        /// The GetUsn.
        /// </summary>
        /// <param name="udn">The udn<see cref="string"/>.</param>
        /// <param name="fullDeviceType">The fullDeviceType<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private string GetUsn(string udn, string fullDeviceType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}::{1}", udn, fullDeviceType);
        }

        /// <summary>
        /// The SendSearchResponse.
        /// </summary>
        /// <param name="searchTarget">The searchTarget<see cref="string"/>.</param>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="uniqueServiceName">The uniqueServiceName<see cref="string"/>.</param>
        /// <param name="endPoint">The endPoint<see cref="IPEndPoint"/>.</param>
        /// <param name="receivedOnlocalIpAddress">The receivedOnlocalIpAddress<see cref="IPAddress"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        private async void SendSearchResponse(
            string searchTarget,
            SsdpDevice device,
            string uniqueServiceName,
            IPEndPoint endPoint,
            IPAddress receivedOnlocalIpAddress,
            CancellationToken cancellationToken)
        {
            var rootDevice = device.ToRootDevice();

            //var additionalheaders = FormatCustomHeadersForResponse(device);

            const string header = "HTTP/1.1 200 OK";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = "",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _OSName, _OSVersion, ServerVersion),
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location.ToString()
            };

            var message = BuildMessage(header, values);

            try
            {
                await _CommsServer.SendMessage(
                        System.Text.Encoding.UTF8.GetBytes(message),
                        endPoint,
                        receivedOnlocalIpAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {

            }

            _logger.LogInformation("Sent search response to {0} : {1}", endPoint, device);
        }

        /// <summary>
        /// The IsDuplicateSearchRequest.
        /// </summary>
        /// <param name="searchTarget">The searchTarget<see cref="string"/>.</param>
        /// <param name="endPoint">The endPoint<see cref="IPEndPoint"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
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
                        _RecentSearchRequests[newRequest.Key] = newRequest;
                    else
                        isDuplicateRequest = true;
                }
                else
                {
                    _RecentSearchRequests.Add(newRequest.Key, newRequest);
                    if (_RecentSearchRequests.Count > 10)
                        CleanUpRecentSearchRequestsAsync();
                }
            }

            return isDuplicateRequest;
        }

        /// <summary>
        /// The CleanUpRecentSearchRequestsAsync.
        /// </summary>
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

        /// <summary>
        /// The SendAllAliveNotifications.
        /// </summary>
        /// <param name="state">The state<see cref="object"/>.</param>
        private void SendAllAliveNotifications(object state)
        {
            try
            {
                if (IsDisposed) return;

                _logger.LogInformation("Begin Sending Alive Notifications For All Devices");

                SsdpRootDevice[] devices;
                lock (_Devices)
                {
                    devices = _Devices.ToArray();
                }

                foreach (var device in devices)
                {
                    if (IsDisposed) return;

                    SendAliveNotifications(device, true, CancellationToken.None);
                }

                _logger.LogInformation("Completed Sending Alive Notifications For All Devices");
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError("Publisher stopped, exception {0}.", ex.Message);
                Dispose();
            }
        }

        /// <summary>
        /// The SendAliveNotifications.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="isRoot">The isRoot<see cref="bool"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        private void SendAliveNotifications(SsdpDevice device, bool isRoot, CancellationToken cancellationToken)
        {
            if (isRoot)
            {
                SendAliveNotification(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice), cancellationToken);
                if (this.SupportPnpRootDevice)
                    SendAliveNotification(device, SsdpConstants.PnpDeviceTypeRootDevice, GetUsn(device.Udn, SsdpConstants.PnpDeviceTypeRootDevice), cancellationToken);
            }

            SendAliveNotification(device, device.Udn, device.Udn, cancellationToken);
            SendAliveNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType), cancellationToken);

            foreach (var childDevice in device.Devices)
            {
                SendAliveNotifications(childDevice, false, cancellationToken);
            }
        }

        /// <summary>
        /// The SendAliveNotification.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="notificationType">The notificationType<see cref="string"/>.</param>
        /// <param name="uniqueServiceName">The uniqueServiceName<see cref="string"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        private void SendAliveNotification(SsdpDevice device, string notificationType, string uniqueServiceName, CancellationToken cancellationToken)
        {
            var rootDevice = device.ToRootDevice();

            const string header = "NOTIFY * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = rootDevice.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ?
                $"{SsdpConstants.MulticastLocalAdminAddress}:{SsdpConstants.MulticastPort}" :
                $"{SsdpConstants.MulticastLocalAdminAddressV6}:{SsdpConstants.MulticastPort}",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["LOCATION"] = rootDevice.Location.ToString(),
                ["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _OSName, _OSVersion, ServerVersion),
                ["NTS"] = SsdpConstants.SsdpKeepAliveNotification,
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage(header, values);

            _CommsServer.SendMulticastMessage(message, _sendOnlyMatchedHost ? rootDevice.Address : null, cancellationToken);

            _logger.LogInformation("Sent alive notification : {0} - {1}", device.FriendlyName, device.SerialNumber);
        }

        /// <summary>
        /// The SendByeByeNotifications.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="isRoot">The isRoot<see cref="bool"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private Task SendByeByeNotifications(SsdpDevice device, bool isRoot, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            if (isRoot)
            {
                tasks.Add(SendByeByeNotification(device,
                    SsdpConstants.UpnpDeviceTypeRootDevice,
                    GetUsn(device.Udn, SsdpConstants.UpnpDeviceTypeRootDevice),
                    cancellationToken));
                if (this.SupportPnpRootDevice)
                    tasks.Add(SendByeByeNotification(device, SsdpConstants.UpnpDeviceTypeRootDevice, GetUsn(device.Udn,SsdpConstants.PnpDeviceTypeRootDevice), cancellationToken));
            }

            tasks.Add(SendByeByeNotification(device, device.Udn, device.Udn, cancellationToken));
            tasks.Add(SendByeByeNotification(
                device,
                String.Format(CultureInfo.InvariantCulture, "urn:{0}", device.FullDeviceType),
                GetUsn(device.Udn, device.FullDeviceType), cancellationToken));

            foreach (var childDevice in device.Devices)
            {
                tasks.Add(SendByeByeNotifications(childDevice, false, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// The SendByeByeNotification.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="notificationType">The notificationType<see cref="string"/>.</param>
        /// <param name="uniqueServiceName">The uniqueServiceName<see cref="string"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "byebye", Justification = "Correct value for this type of notification in SSDP.")]
        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName, CancellationToken cancellationToken)
        {
            const string header = "NOTIFY * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // If needed later for non-server devices, these headers will need to be dynamic
                ["HOST"] = device.ToRootDevice().Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ?
                $"{SsdpConstants.MulticastLocalAdminAddress}:{SsdpConstants.MulticastPort}" :
                $"{SsdpConstants.MulticastLocalAdminAddressV6}:{SsdpConstants.MulticastPort}",
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["SERVER"] = string.Format(CultureInfo.InvariantCulture, "{0}/{1} UPnP/1.0 RSSDP/{2}", _OSName, _OSVersion, ServerVersion),
                ["NTS"] = SsdpConstants.SsdpByeByeNotification,
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName
            };

            var message = BuildMessage(header, values);

            var sendCount = IsDisposed ? 1 : 3;
            _logger.LogInformation("Sent byebye notification : {0}", device);
            return _CommsServer.SendMulticastMessage(message, sendCount, _sendOnlyMatchedHost ? device.ToRootDevice().Address : null, cancellationToken);
        }

        /// <summary>
        /// The DisposeRebroadcastTimer.
        /// </summary>
        private void DisposeRebroadcastTimer()
        {
            var timer = _RebroadcastAliveNotificationsTimer;
            _RebroadcastAliveNotificationsTimer = null;
            if (timer != null)
                timer.Dispose();
        }

        /// <summary>
        /// The GetMinimumNonZeroCacheLifetime.
        /// </summary>
        /// <returns>The <see cref="TimeSpan"/>.</returns>
        private TimeSpan GetMinimumNonZeroCacheLifetime()
        {
            var nonzeroCacheLifetimesQuery = (from device in _Devices
                                              where device.CacheLifetime != TimeSpan.Zero
                                              select device.CacheLifetime).ToList();

            if (nonzeroCacheLifetimesQuery.Any())
                return nonzeroCacheLifetimesQuery.Min();
            else
                return TimeSpan.Zero;
        }

        /// <summary>
        /// The GetFirstHeaderValue.
        /// </summary>
        /// <param name="httpRequestHeaders">The httpRequestHeaders<see cref="System.Net.Http.Headers.HttpRequestHeaders"/>.</param>
        /// <param name="headerName">The headerName<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private string GetFirstHeaderValue(System.Net.Http.Headers.HttpRequestHeaders httpRequestHeaders, string headerName)
        {
            string retVal = null;
            IEnumerable<String> values;
            if (httpRequestHeaders.TryGetValues(headerName, out values) && values != null)
                retVal = values.FirstOrDefault();

            return retVal;
        }
       
        /// <summary>
        /// Expands the device's name.
        /// </summary>        
        /// <param name="device">The device to expand.</param>
        private string Expand(SsdpDevice device)
        {
            var rootDevice = device as SsdpRootDevice;
            if (rootDevice != null)
                return device.DeviceType + " - " + device.Uuid + " - " + rootDevice.Location;
            else
                return device.DeviceType + " - " + device.Uuid;
        }

        /// <summary>
        /// The CommsServer_RequestReceived.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RequestReceivedEventArgs"/>.</param>
        private void CommsServer_RequestReceived(object sender, RequestReceivedEventArgs e)
        {
            if (this.IsDisposed) return;

            if (string.Equals(e.Message.Method.Method, SsdpConstants.MSearchMethod, StringComparison.OrdinalIgnoreCase))
            {
                //According to SSDP/UPnP spec, ignore message if missing these headers.
                // Edit: But some devices do it anyway
                //if (!e.Message.Headers.Contains("MX"))
                //    WriteTrace("Ignoring search request - missing MX header.");
                //else if (!e.Message.Headers.Contains("MAN"))
                //    WriteTrace("Ignoring search request - missing MAN header.");
                //else

                // Only process requests that don't originate from ourselves.
                ProcessSearchRequest(GetFirstHeaderValue(e.Message.Headers, "MX"), GetFirstHeaderValue(e.Message.Headers, "ST"), e.ReceivedFrom, e.LocalIpAddress, CancellationToken.None);                
            }
        }

        /// <summary>
        /// Defines the <see cref="SearchRequest" />.
        /// </summary>
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

            /// <summary>
            /// The IsOld.
            /// </summary>
            /// <returns>The <see cref="bool"/>.</returns>
            public bool IsOld()
            {
                return DateTime.UtcNow.Subtract(this.Received).TotalMilliseconds > 500;
            }
        }
    }
}
