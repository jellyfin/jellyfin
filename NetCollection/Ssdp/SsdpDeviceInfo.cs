using System;
using System.Collections.Generic;
using System.Net;

namespace NetworkCollection.Ssdp
{
    /// <summary>
    /// Defines the <see cref="UpnpDeviceInfo" />.
    /// </summary>
    public class SsdpDeviceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpDeviceInfo"/> class.
        /// </summary>
        /// <param name="location">The <see cref="Uri"/>.</param>
        /// <param name="headers">The <see cref="Dictionary{string, string}"/>.</param>
        /// <param name="localIpAddress">The <see cref="IPAddress"/>.</param>
        public SsdpDeviceInfo(Uri? location, Dictionary<string, string> headers, IPAddress localIpAddress)
        {
            Location = location;
            Headers = headers;
            LocalIpAddress = localIpAddress;
        }

        /// <summary>
        /// Gets the Location
        /// Gets or sets the Location..
        /// </summary>
        public Uri? Location { get; }

        /// <summary>
        /// Gets the Headers
        /// Gets or sets the Headers..
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets or sets the LocalIpAddress.
        /// </summary>
        public IPAddress LocalIpAddress { get; set; }
    }
}
