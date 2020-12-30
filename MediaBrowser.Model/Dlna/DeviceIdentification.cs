using System;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceIdentification" />.
    /// </summary>
    public class DeviceIdentification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceIdentification"/> class.
        /// </summary>
        public DeviceIdentification()
        {
            Headers = Array.Empty<HttpHeaderInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceIdentification"/> class.
        /// </summary>
        /// <param name="friendlyName">The friendlyName.</param>
        /// <param name="headers">Array of <see cref="HttpHeaderInfo"/>.</param>
        public DeviceIdentification(string? friendlyName, HttpHeaderInfo[] headers)
        {
            Headers = headers;
            FriendlyName = friendlyName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceIdentification"/> class.
        /// </summary>
        /// <param name="friendlyName">The friendlyName.</param>
        public DeviceIdentification(string? friendlyName)
        {
            FriendlyName = friendlyName;
            Headers = Array.Empty<HttpHeaderInfo>();
        }

        /// <summary>
        /// Gets or sets the name of the friendly..
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the model number.
        /// </summary>
        public string? ModelNumber { get; set; }

        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Gets or sets the model description.
        /// </summary>
        public string? ModelDescription { get; set; }

        /// <summary>
        /// Gets or sets the model URL.
        /// </summary>
        public string? ModelUrl { get; set; }

        /// <summary>
        /// Gets or sets the Manufacturer.
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer URL.
        /// </summary>
        public string? ManufacturerUrl { get; set; }

        /// <summary>
        /// Gets or sets the Headers.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public HttpHeaderInfo[] Headers { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
