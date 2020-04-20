using System;
using System.Net.Http.Headers;

namespace Rssdp
{
    /// <summary>
    /// Represents a discovered device, containing basic information about the device and the location of it's full device description document. Also provides convenience methods for retrieving the device description document.
    /// </summary>
    /// <seealso cref="SsdpDevice"/>
    /// <seealso cref="Infrastructure.ISsdpDeviceLocator"/>
    public sealed class DiscoveredSsdpDevice
    {

        #region Fields

        private DateTimeOffset _AsAt;

        #endregion

        #region Public Properties

        /// <summary>
        /// Sets or returns the type of notification, being either a uuid, device type, service type or upnp:rootdevice.
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// Sets or returns the universal service name (USN) of the device.
        /// </summary>
        public string Usn { get; set; }

        /// <summary>
        /// Sets or returns a URL pointing to the device description document for this device.
        /// </summary>
        public Uri DescriptionLocation { get; set; }

        /// <summary>
        /// Sets or returns the length of time this information is valid for (from the <see cref="AsAt"/> time).
        /// </summary>
        public TimeSpan CacheLifetime { get; set; }

        /// <summary>
        /// Sets or returns the date and time this information was received.
        /// </summary>
        public DateTimeOffset AsAt
        {
            get { return _AsAt; }
            set
            {
                if (_AsAt != value)
                {
                    _AsAt = value;
                }
            }
        }

        /// <summary>
        /// Returns the headers from the SSDP device response message
        /// </summary>
        public HttpHeaders ResponseHeaders { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns true if this device information has expired, based on the current date/time, and the <see cref="CacheLifetime"/> &amp; <see cref="AsAt"/> properties.
        /// </summary>
        /// <returns></returns>
        public bool IsExpired()
        {
            return this.CacheLifetime == TimeSpan.Zero || this.AsAt.Add(this.CacheLifetime) <= DateTimeOffset.Now;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Returns the device's <see cref="Usn"/> value.
        /// </summary>
        /// <returns>A string containing the device's universal service name.</returns>
        public override string ToString()
        {
            return this.Usn;
        }

        #endregion
    }
}
