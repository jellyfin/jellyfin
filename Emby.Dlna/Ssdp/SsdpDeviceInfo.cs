#nullable enable
using System;
using System.Collections.Generic;
using System.Net;

namespace Emby.Dlna.Ssdp
{
    /// <summary>
    /// Defines the <see cref="SsdpDeviceInfo" />.
    /// </summary>
    public class SsdpDeviceInfo
    {
#pragma warning disable CS1584 // XML comment has syntactically incorrect cref attribute
#pragma warning disable CS1658 // Warning is overriding an error
        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpDeviceInfo"/> class.
        /// </summary>
        /// <param name="location">The <see cref="Uri"/>.</param>
        /// <param name="headers">The <see cref="Dictionary{string, string}"/>.</param>
        /// <param name="localIpAddress">The <see cref="IPAddress"/>.</param>
        public SsdpDeviceInfo(Uri? location, Dictionary<string, string> headers, IPAddress localIpAddress)
#pragma warning restore CS1658 // Warning is overriding an error
#pragma warning restore CS1584 // XML comment has syntactically incorrect cref attribute
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
