#nullable enable
using System;
using Emby.Dlna.Configuration;
using Emby.Dlna.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Locates SsdpPlayTo ssdp devices.
    /// </summary>
    public class SsdpPlayToLocator : SsdpLocator, ISsdpPlayToLocator
    {
        private readonly IConfigurationManager _configuration;

#pragma warning disable CA1062 // Validate arguments of public methods
        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpPlayToLocator"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
        public SsdpPlayToLocator(ILogger logger, INetworkManager networkManager, IConfigurationManager configurationManager)
        : base(
            logger,
            networkManager.GetInternalBindAddresses(),
            new string[] { "urn:schemas-upnp-org:device:MediaRenderer:" },
            true,
            networkManager.IsInLocalNetwork,
            networkManager.IsIP4Enabled,
            networkManager.IsIP6Enabled)
#pragma warning restore CA1062 // Validate arguments of public methods
        {
            _configuration = configurationManager;
        }

        /// <summary>
        /// Raised when a new device is discovered.
        /// </summary>
        public event EventHandler<SsdpDeviceInfo>? DeviceDiscovered;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        public event EventHandler<SsdpDeviceInfo>? DeviceLeft;

        /// <inheritdoc/>
        public override void Start()
        {
            Interval = _configuration.GetDlnaConfiguration().AliveMessageIntervalSeconds;
            base.Start();
        }

        /// <inheritdoc/>
        protected override void DeviceDiscoveredEvent(bool isNewDevice, SsdpDeviceInfo args)
        {
            DeviceDiscovered?.Invoke(this, args);
        }

        /// <inheritdoc/>
        protected override void DeviceLeftEvent(SsdpDeviceInfo args)
        {
            DeviceLeft?.Invoke(this, args);
        }
    }
}
