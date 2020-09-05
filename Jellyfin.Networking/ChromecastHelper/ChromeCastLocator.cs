using System;
using System.Linq;
using System.Net;
using Jellyfin.Data.Events;
using Jellyfin.Networking.Manager;
using Jellyfin.Networking.Ssdp;
using Jellyfin.Networking.Structures;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Net
{
    /// <summary>
    /// ChromecastLocator device locator.
    /// </summary>
    public class ChromecastLocator : SsdpLocator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChromecastLocator"/> class.
        /// </summary>
        /// <param name="logger">ILogger instance.</param>
        /// <param name="configurationManager">Configuration manager instance.</param>
        /// <param name="applicationHost">Application Host instance.</param>
        public ChromecastLocator(
            ILogger logger,
            IConfigurationManager configurationManager,
            IServerApplicationHost applicationHost)
        : base(
            logger,
            configurationManager,
            applicationHost,
            new string[] { "urn:dial-multiscreen-org:device:dial:1", "urn:dial-multiscreen-org:service:dial:1" },
            false)
        {
        }

        /// <inheritdoc/>
        protected override void DeviceDiscoveredEvent(bool isNewDevice, GenericEventArgs<UpnpDeviceInfo> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var urls = NetworkManager.Instance.PublishedServerOverrides;
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
            NetworkManager.Instance.PublishedServerOverrides.Remove(key);
        }
    }
}
