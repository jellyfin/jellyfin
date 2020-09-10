#nullable enable
using System;
using System.Net;
using NetworkCollection;

namespace Emby.Dlna.PlayTo.Devices
{
    /// <summary>
    /// Represents a 'root' device, a device that has no parent. Used for publishing devices and for the root device in a tree of discovered devices.
    /// </summary>
    /// <remarks>
    /// <para>Child (embedded) devices are represented by the <see cref="SsdpDevice"/> in the <see cref="SsdpDevice.Devices"/> property.</para>
    /// <para>Root devices contain some information that applies to the whole device tree and is therefore not present on child devices, such as <see cref="CacheLifetime"/> and <see cref="Location"/>.</para>
    /// </remarks>
    /// <remarks>
    /// Part of this code are taken from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public class SsdpRootDevice : SsdpDevice, IEquatable<SsdpRootDevice>
    {
        private Uri? _urlBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpRootDevice"/> class.
        /// </summary>
        /// <param name="cacheLifetime">Cache lifetime.</param>
        /// <param name="location">Location.</param>
        /// <param name="address">IP Address.</param>
        /// <param name="friendlyName">Friendly name.</param>
        /// <param name="manufacturer">Manufacturer.</param>
        /// <param name="modelName">Model name.</param>
        /// <param name="uuid">UDN.</param>
        public SsdpRootDevice(TimeSpan cacheLifetime, Uri location, IPObject address, string friendlyName, string manufacturer, string modelName, string uuid)
            : base(friendlyName, manufacturer, modelName, uuid)
        {
            CacheLifetime = cacheLifetime;
            Location = location;
            NetAddress = address;
        }

        /// <summary>
        /// Gets or sets specifies how long clients can cache this device's details for. Optional but defaults to <see cref="TimeSpan.Zero"/> which means no-caching.
        /// Recommended value is half an hour.
        /// </summary>
        /// <remarks>
        /// <para>Specifiy <see cref="TimeSpan.Zero"/> to indicate no caching allowed.</para>
        /// <para>Also used to specify how often to rebroadcast alive notifications.</para>
        /// <para>The UPnP/SSDP specifications indicate this should not be less than 1800 seconds (half an hour), but this is not enforced by this library.</para>
        /// </remarks>
        public TimeSpan CacheLifetime { get; set; }

        /// <summary>
        /// Gets or sets the URL used to retrieve the description document for this device/tree. Required.
        /// </summary>
        public Uri Location { get; set; }

        /// <summary>
        /// Gets or sets the IP Object Address used to check if the received message from same interface with this device/tree. Required.
        /// </summary>
        public IPObject NetAddress { get; set; }

        /// <summary>
        /// Gets the Address used to check if the received message from same interface with this device/tree.
        /// </summary>
        public IPAddress Address { get => NetAddress.Address; }

        /// <summary>
        /// Gets or sets the base URL to use for all relative url's provided in other propertise (and those of child devices). Optional.
        /// </summary>
        /// <remarks>
        /// <para>Defines the base URL. Used to construct fully-qualified URLs. All relative URLs that appear elsewhere in the description are combined with this base URL. If URLBase is empty or not given, the base URL is the URL from which the device description was retrieved (which is the preferred implementation; use of URLBase is no longer recommended). Specified by UPnP vendor. Single URL.</para>
        /// </remarks>
        public Uri UrlBase
        {
            get
            {
                return _urlBase ?? this.Location;
            }

            set
            {
                _urlBase = value;
            }
        }

        /// <summary>
        /// Returns this object as a string.
        /// </summary>
        /// <returns>String representation of this object.</returns>
        public override string ToString()
        {
            return $"{DeviceType} - {Uuid} - {Location}";
        }

        /// <summary>
        /// Used by List{SsdpRoot}.Contains.
        /// </summary>
        /// <param name="other">Item to compare.</param>
        /// <returns>True if other matches this object.</returns>
        public bool Equals(SsdpRootDevice other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase) && NetAddress.Equals(other.NetAddress);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is SsdpRootDevice && Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
    }
}
