using System;
using System.Net;

namespace Rssdp
{
    /// <summary>
    /// Represents a 'root' device, a device that has no parent. Used for publishing devices and for the root device in a tree of discovered devices.
    /// </summary>
    /// <remarks>
    /// <para>Child (embedded) devices are represented by the <see cref="SsdpDevice"/> in the <see cref="SsdpDevice.Devices"/> property.</para>
    /// <para>Root devices contain some information that applies to the whole device tree and is therefore not present on child devices, such as <see cref="CacheLifetime"/> and <see cref="Location"/>.</para>
    /// </remarks>
    public class SsdpRootDevice : SsdpDevice
    {
        #region Fields

        private Uri _UrlBase;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SsdpRootDevice() : base()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Specifies how long clients can cache this device's details for. Optional but defaults to <see cref="TimeSpan.Zero"/> which means no-caching. Recommended value is half an hour.
        /// </summary>
        /// <remarks>
        /// <para>Specifiy <see cref="TimeSpan.Zero"/> to indicate no caching allowed.</para>
        /// <para>Also used to specify how often to rebroadcast alive notifications.</para>
        /// <para>The UPnP/SSDP specifications indicate this should not be less than 1800 seconds (half an hour), but this is not enforced by this library.</para>
        /// </remarks>
        public TimeSpan CacheLifetime
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the URL used to retrieve the description document for this device/tree. Required.
        /// </summary>
        public Uri Location { get; set; }

        /// <summary>
        /// Gets or sets the Address used to check if the received message from same interface with this device/tree. Required.
        /// </summary>
        public IPAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the SubnetMask used to check if the received message from same interface with this device/tree. Required.
        /// </summary>
        public IPAddress SubnetMask { get; set; }

        /// <summary>
        /// The base URL to use for all relative url's provided in other propertise (and those of child devices). Optional.
        /// </summary>
        /// <remarks>
        /// <para>Defines the base URL. Used to construct fully-qualified URLs. All relative URLs that appear elsewhere in the description are combined with this base URL. If URLBase is empty or not given, the base URL is the URL from which the device description was retrieved (which is the preferred implementation; use of URLBase is no longer recommended). Specified by UPnP vendor. Single URL.</para>
        /// </remarks>
        public Uri UrlBase
        {
            get
            {
                return _UrlBase ?? this.Location;
            }

            set
            {
                _UrlBase = value;
            }
        }

        #endregion
    }
}
