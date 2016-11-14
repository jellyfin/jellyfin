using MediaBrowser.Common.Events;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Threading;
using Rssdp;
using Rssdp.Infrastructure;

namespace Emby.Dlna.Ssdp
{
    public class DeviceDiscovery : IDeviceDiscovery, IDisposable
    {
        private bool _disposed;

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly CancellationTokenSource _tokenSource;

        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;

        private SsdpDeviceLocator _deviceLocator;

        private readonly ITimerFactory _timerFactory;
        private readonly ISocketFactory _socketFactory;

        public DeviceDiscovery(ILogger logger, IServerConfigurationManager config, ISocketFactory socketFactory, ITimerFactory timerFactory)
        {
            _tokenSource = new CancellationTokenSource();

            _logger = logger;
            _config = config;
            _socketFactory = socketFactory;
            _timerFactory = timerFactory;
        }

        // Call this method from somewhere in your code to start the search.
        public void Start(ISsdpCommunicationsServer communicationsServer)
        {
            _deviceLocator = new SsdpDeviceLocator(communicationsServer, _timerFactory);

            // (Optional) Set the filter so we only see notifications for devices we care about 
            // (can be any search target value i.e device type, uuid value etc - any value that appears in the 
            // DiscoverdSsdpDevice.NotificationType property or that is used with the searchTarget parameter of the Search method).
            //_DeviceLocator.NotificationFilter = "upnp:rootdevice";

            // Connect our event handler so we process devices as they are found
            _deviceLocator.DeviceAvailable += deviceLocator_DeviceAvailable;
            _deviceLocator.DeviceUnavailable += _DeviceLocator_DeviceUnavailable;

            // Perform a search so we don't have to wait for devices to broadcast notifications 
            // again to get any results right away (notifications are broadcast periodically).
            StartAsyncSearch();
        }

        private void StartAsyncSearch()
        {
            Task.Factory.StartNew(async (o) =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        // Enable listening for notifications (optional)
                        _deviceLocator.StartListeningForNotifications();

                        await _deviceLocator.SearchAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error searching for devices", ex);
                    }

                    var delay = _config.GetDlnaConfiguration().ClientDiscoveryIntervalSeconds * 1000;

                    await Task.Delay(delay, _tokenSource.Token).ConfigureAwait(false);
                }

            }, CancellationToken.None, TaskCreationOptions.LongRunning);
        }

        // Process each found device in the event handler
        void deviceLocator_DeviceAvailable(object sender, DeviceAvailableEventArgs e)
        {
            var originalHeaders = e.DiscoveredDevice.ResponseHeaders;

            var headerDict = originalHeaders == null ? new Dictionary<string, KeyValuePair<string, IEnumerable<string>>>() : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

            var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

            var args = new GenericEventArgs<UpnpDeviceInfo>
            {
                Argument = new UpnpDeviceInfo
                {
                    Location = e.DiscoveredDevice.DescriptionLocation,
                    Headers = headers
                }
            };

            EventHelper.FireEventIfNotNull(DeviceDiscovered, this, args, _logger);
        }

        private void _DeviceLocator_DeviceUnavailable(object sender, DeviceUnavailableEventArgs e)
        {
            var originalHeaders = e.DiscoveredDevice.ResponseHeaders;

            var headerDict = originalHeaders == null ? new Dictionary<string, KeyValuePair<string, IEnumerable<string>>>() : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

            var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

            var args = new GenericEventArgs<UpnpDeviceInfo>
            {
                Argument = new UpnpDeviceInfo
                {
                    Location = e.DiscoveredDevice.DescriptionLocation,
                    Headers = headers
                }
            };

            EventHelper.FireEventIfNotNull(DeviceLeft, this, args, _logger);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _tokenSource.Cancel();
            }
        }
    }
}
