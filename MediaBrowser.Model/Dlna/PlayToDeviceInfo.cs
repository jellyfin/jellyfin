#nullable enable
#pragma warning disable CS1591
#pragma warning disable CA1819

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class PlayToDeviceInfo
    {
        public PlayToDeviceInfo()
        {
            FriendlyName = "Generic Device";
        }

        public string UUID { get; set; } = string.Empty;

        public string FriendlyName { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

        public string ModelNumber { get; set; } = string.Empty;

        public string ModelDescription { get; set; } = string.Empty;

        public string ModelUrl { get; set; } = string.Empty;

        public string Manufacturer { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string ManufacturerUrl { get; set; } = string.Empty;

        public string PresentationUrl { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public DeviceIcon? Icon { get; set; }

        public List<DeviceService> Services { get; } = new List<DeviceService>();

        public string Capabilities { get; set; } = string.Empty;
    }
}
