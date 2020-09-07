#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public class PlayToDeviceInfo
    {
        public string UUID { get; set; } = string.Empty;

        public string Name { get; set; } = "Generic Device";

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
    }
}
