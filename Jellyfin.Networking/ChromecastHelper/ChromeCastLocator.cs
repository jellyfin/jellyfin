using System;
using System.Linq;
using System.Net;
using Jellyfin.Data.Events;
using Jellyfin.Networking.Manager;
using Jellyfin.Networking.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;
using NetworkCollection;

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
        /// <param name="networkManager">The <see cref="NetworkManager"/> instance.</param>
        /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
        /// <param name="applicationHost">The <see cref="IServerApplicationHost"/> instance.</param>
        public ChromecastLocator(
            ILogger logger,
            INetworkManager networkManager,
            IConfigurationManager configurationManager,
            IServerApplicationHost applicationHost)
        : base(
            logger,
            networkManager,
            configurationManager,
            applicationHost,
            new string[] { "urn:dial-multiscreen-org:device:dial:1", "urn:dial-multiscreen-org:service:dial:1" },
            false)
        {
            _networkManager = networkManager;
        }

        /// <inheritdoc/>
        protected override void DeviceDiscoveredEvent(bool isNewDevice, GenericEventArgs<UpnpDeviceInfo> args)
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
                var key = new IPNetAddress(args.Argument.LocalIpAddress);
                if (!urls.ContainsKey(key))
                {
                    urls[key] = externalAddress;
                }
            }
        }

        /// <inheritdoc/>
        protected override void DeviceLeftEvent(GenericEventArgs<UpnpDeviceInfo> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var key = new IPNetAddress(args.Argument.LocalIpAddress);
            _networkManager.PublishedServerUrls.Remove(key);
        }
    }
}
