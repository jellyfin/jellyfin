using System;

using SddpMessage = System.Collections.Generic.Dictionary<string, string>;

namespace NetworkCollection.Ssdp
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
        public DiscoveredSsdpDevice(DateTimeOffset asAt, string notificationType, SddpMessage messageHeaders)
        {
            if (messageHeaders == null)
            {
                throw new ArgumentNullException(nameof(messageHeaders));
            }

            try
            {
                AsAt = asAt;
                CacheLifetime = TimeSpan.Zero;

                if (messageHeaders.TryGetValue("CACHE-CONTROL", out var cc))
                {
                    if (!string.IsNullOrEmpty(cc))
                    {
                        var values = cc.Split('=');
                        if (string.Equals("Max-Age", values[0], StringComparison.OrdinalIgnoreCase) || string.Equals("Shared-MaxAge", values[0], StringComparison.OrdinalIgnoreCase))
                        {
                            if (TimeSpan.TryParse(values[1], out TimeSpan clt))
                            {
                                CacheLifetime = clt;
                            }
                        }
                    }
                }

                DescriptionLocation = new Uri(messageHeaders["LOCATION"], UriKind.RelativeOrAbsolute);

                NotificationType = messageHeaders[notificationType];
                Usn = messageHeaders["USN"];
                Headers = messageHeaders;
            }
            catch
            {
                throw new ArgumentException("Invalid structure passed to DiscoveredSsdpDevice\r\n{0}", SsdpServer.DebugOutput(messageHeaders));
            }
        }

        /// <summary>
        /// Gets the type of notification, being either a uuid, device type, service type or upnp:rootdevice.
        /// </summary>
        public string NotificationType { get; }

        /// <summary>
        /// Gets the universal service name (USN) of the device.
        /// </summary>
        public string Usn { get; }

        /// <summary>
        /// Gets a URL pointing to the device description document for this device.
        /// </summary>
        public Uri DescriptionLocation { get; }

        /// <summary>
        /// Gets the length of time this information is valid for (from the <see cref="AsAt"/> time).
        /// </summary>
        public TimeSpan CacheLifetime { get; }

        /// <summary>
        /// Gets the date and time this information was received.
        /// </summary>
        public DateTimeOffset AsAt { get; }

        /// <summary>
        /// Gets the headers from the SSDP device response message.
        /// </summary>
        public SddpMessage Headers { get; }

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
