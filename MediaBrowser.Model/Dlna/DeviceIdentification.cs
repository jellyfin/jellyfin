using System;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceIdentification" />.
    /// </summary>
    public class DeviceIdentification
    {
        /// <summary>
        /// Gets or sets the name of the friendly.
        /// </summary>
        public string FriendlyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model number.
        /// </summary>
        public string ModelNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model description.
        /// </summary>
        public string ModelDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model URL.
        /// </summary>
        public string ModelUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Manufacturer.
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the manufacturer URL.
        /// </summary>
        public string ManufacturerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Headers.
        /// </summary>
        public HttpHeaderInfo[] Headers { get; set; } = Array.Empty<HttpHeaderInfo>();
    }
}
