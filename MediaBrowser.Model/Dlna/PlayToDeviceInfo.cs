#nullable enable
#pragma warning disable CA1819

using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="PlayToDeviceInfo" />.
    /// </summary>
    public class PlayToDeviceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayToDeviceInfo"/> class.
        /// </summary>
        public PlayToDeviceInfo()
        {
            Name = "Generic Device";
        }

        /// <summary>
        /// Gets or sets the UUID.
        /// </summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Model Name.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Model Number.
        /// </summary>
        public string ModelNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Model Description.
        /// </summary>
        public string ModelDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Model Url.
        /// </summary>
        public string ModelUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Manufacturer.
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Serial Number.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Manufacturer Url.
        /// </summary>
        public string ManufacturerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Presentation Url.
        /// </summary>
        public string PresentationUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Base Url.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Icon.
        /// </summary>
        public DeviceIcon? Icon { get; set; }

        /// <summary>
        /// Gets the services the device supports.
        /// </summary>
        public List<DeviceService> Services { get; } = new List<DeviceService>();

        /// <summary>
        /// Gets or sets the capabilities of the device.
        /// </summary>
        public string Capabilities { get; set; } = string.Empty;
    }
}
