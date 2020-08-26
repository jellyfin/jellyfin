#nullable enable
using System;
using System.Net.Http.Headers;
using Emby.Dlna.Net.Parsers;

namespace Emby.Dlna.PlayTo.Devices
{
    /// <summary>
    /// Represents a discovered device, containing basic information about the device and the location of it's full device description document. Also provides convenience methods for retrieving the device description document.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public class DiscoveredSsdpDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveredSsdpDevice"/> class.
        /// </summary>
        /// <param name="asAt">Time data was received.</param>
        /// <param name="notificationType">Header name used for the notification type.</param>
        /// <param name="messageHeaders">Message headers.</param>
        public DiscoveredSsdpDevice(DateTimeOffset asAt, string notificationType, HttpHeaders messageHeaders)
        {
            AsAt = asAt;
            if (messageHeaders is HttpRequestHeaders mhr)
            {
                CacheLifetime = SsdpMessageHelper.CacheAgeFromHeader(mhr.CacheControl);
            }
            else
            {
                CacheLifetime = SsdpMessageHelper.CacheAgeFromHeader(((HttpResponseHeaders)messageHeaders).CacheControl);
            }

            DescriptionLocation = SsdpMessageHelper.GetFirstHeaderUriValue("Location", messageHeaders);
            NotificationType = SsdpMessageHelper.GetFirstHeaderValue(notificationType, messageHeaders);
            // Nls = SsdpMessageHelper.GetFirstHeaderValue("NLS", messageHeaders);
            Usn = SsdpMessageHelper.GetFirstHeaderValue("USN", messageHeaders);
            Headers = messageHeaders;
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
        public HttpHeaders Headers { get; set; }

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
