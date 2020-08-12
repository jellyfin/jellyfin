#nullable enable
using System;
using System.Net.Http.Headers;

namespace Emby.Dlna.Rssdp.Devices
{
    /// <summary>
    /// Represents a discovered device, containing basic information about the device and the location of it's full device description document. Also provides convenience methods for retrieving the device description document.
    /// </summary>
    public sealed class DiscoveredSsdpDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveredSsdpDevice"/> class.
        /// </summary>
        /// <param name="asAt">Time data was received.</param>
        /// <param name="cacheLifetime">Length of time this is valid.</param>
        /// <param name="descriptionLocation">Description location.</param>
        /// <param name="notificationType">Notification type.</param>
        /// <param name="usn">Universal Service Name.</param>
        /// <param name="responseHeaders">Response headers.</param>
        public DiscoveredSsdpDevice(DateTimeOffset asAt, TimeSpan cacheLifetime, Uri? descriptionLocation, string notificationType, string usn, HttpHeaders responseHeaders)
        {
            AsAt = asAt;
            CacheLifetime = cacheLifetime;
            DescriptionLocation = descriptionLocation;
            NotificationType = notificationType;
            Usn = usn;
            ResponseHeaders = responseHeaders;
        }

        /// <summary>
        /// Gets or sets the type of notification, being either a uuid, device type, service type or upnp:rootdevice.
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// Gets or sets the universal service name (USN) of the device.
        /// </summary>
        public string Usn { get; set; }

        /// <summary>
        /// Gets or sets a URL pointing to the device description document for this device.
        /// </summary>
        public Uri? DescriptionLocation { get; set; }

        /// <summary>
        /// Gets or sets the length of time this information is valid for (from the <see cref="AsAt"/> time).
        /// </summary>
        public TimeSpan CacheLifetime { get; set; }

        /// <summary>
        /// Gets or sets the date and time this information was received.
        /// </summary>
        public DateTimeOffset AsAt { get; set; }

        /// <summary>
        /// Gets or sets the headers from the SSDP device response message.
        /// </summary>
        public HttpHeaders ResponseHeaders { get; set; }

        /// <summary>
        /// Returns true if this device information has expired, based on the current date/time, and the <see cref="CacheLifetime"/> &amp; <see cref="AsAt"/> properties.
        /// </summary>
        /// <returns>True of this device information has expired.</returns>
        public bool IsExpired()
        {
            return this.CacheLifetime == TimeSpan.Zero || this.AsAt.Add(this.CacheLifetime) <= DateTimeOffset.Now;
        }

        /// <summary>
        /// Returns the device's <see cref="Usn"/> value.
        /// </summary>
        /// <returns>A string containing the device's universal service name.</returns>
        public override string ToString()
        {
            return this.Usn;
        }
    }
}
