#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public class PlayToDeviceInfo
    {
        public PlayToDeviceInfo()
        {
            Name = "Generic Device";
        }

        public string UUID { get; set; }

        public string Name { get; set; }

        public string ModelName { get; set; }

        public string ModelNumber { get; set; }

        public string ModelDescription { get; set; }

        public string ModelUrl { get; set; }

        public string Manufacturer { get; set; }

        public string SerialNumber { get; set; }

        public string ManufacturerUrl { get; set; }

        public string PresentationUrl { get; set; }

        public string BaseUrl { get; set; } = string.Empty;

        public DeviceIcon Icon { get; set; }

        public List<DeviceService> Services { get; } = new List<DeviceService>();
    }
}
