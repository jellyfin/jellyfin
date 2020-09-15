#nullable enable
using System;
using Emby.Dlna.Configuration;
using Jellyfin.Data.Events;
using Jellyfin.Networking.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Locates SsdpPlayTo ssdp devices.
    /// </summary>
    public class SsdpPlayToLocator : SsdpLocator, ISsdpPlayToLocator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpPlayToLocator"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
        /// <param name="applicationHost">The <see cref="IServerApplicationHost"/> instance.</param>
        public SsdpPlayToLocator(ILogger logger, INetworkManager networkManager, IConfigurationManager configurationManager, IServerApplicationHost applicationHost)
        : base(
            logger,
            networkManager,
            configurationManager,
            applicationHost,
            new string[] { "urn:schemas-upnp-org:device:MediaRenderer:" },
            true)
        {
        }

        /// <summary>
        /// Raised when a new device is discovered.
        /// </summary>
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceDiscovered;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceLeft;

        /// <inheritdoc/>
        public override void Start()
        {
            Interval = Configuration.GetDlnaConfiguration().AliveMessageIntervalSeconds;
            base.Start();
        }

        /// <inheritdoc/>
        protected override void DeviceDiscoveredEvent(bool isNewDevice, GenericEventArgs<UpnpDeviceInfo> args)
        {
            DeviceDiscovered?.Invoke(this, args);
        }

        /// <inheritdoc/>
        protected override void DeviceLeftEvent(GenericEventArgs<UpnpDeviceInfo> args)
        {
            DeviceLeft?.Invoke(this, args);
        }
    }
}
