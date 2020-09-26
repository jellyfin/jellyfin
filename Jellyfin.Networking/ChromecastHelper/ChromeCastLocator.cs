using System;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using NetworkCollection;
using NetworkCollection.Ssdp;
using NetworkCollection.SSDP;

namespace Emby.Dlna.Net
{
    /// <summary>
    /// ChromecastLocator device locator.
    /// </summary>
    public class ChromecastLocator : SsdpLocator
    {
        private readonly INetworkManager _networkManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromecastLocator"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        public ChromecastLocator(
            ILogger logger,
            INetworkManager networkManager)
        : base(
            logger,
            networkManager.GetInternalBindAddresses(),
            new string[] { "urn:dial-multiscreen-org:device:dial:1", "urn:dial-multiscreen-org:service:dial:1" },
            false)
        {
            _networkManager = networkManager;
        }

        /// <inheritdoc/>
        protected override void DeviceDiscoveredEvent(bool isNewDevice, SsdpDeviceInfo args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var urls = _networkManager.PublishedServerUrls;
            // Find the defined external address.
            var externalAddress = urls.Where(i => i.Key.Equals(IPAddress.Any)).FirstOrDefault().Value;

            if (externalAddress != null)
            {
                var key = new IPNetAddress(args.LocalIpAddress);
                if (!urls.ContainsKey(key))
                {
                    urls[key] = externalAddress;
                }
            }
        }

        /// <inheritdoc/>
        protected override void DeviceLeftEvent(SsdpDeviceInfo args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var key = new IPNetAddress(args.LocalIpAddress);
            _networkManager.PublishedServerUrls.Remove(key);
        }
    }
}
