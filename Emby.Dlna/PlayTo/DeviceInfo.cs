#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public class DeviceInfo
    {
        public DeviceInfo()
        {
            Name = "Generic Device";
            UUID = string.Empty;
            Name = string.Empty;
            ModelName = string.Empty;
            ModelDescription = string.Empty;
            ModelUrl = string.Empty;
            Manufacturer = string.Empty;
            SerialNumber = string.Empty;
            ManufacturerUrl = string.Empty;
            PresentationUrl = string.Empty;
            BaseUrl = string.Empty;
            Services = new List<DeviceService>();
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

        public string BaseUrl { get; set; }

        public DeviceIcon? Icon { get; set; }

        public List<DeviceService> Services { get; }

        public DeviceIdentification ToDeviceIdentification()
        {
            return new DeviceIdentification
            {
                Manufacturer = Manufacturer,
                ModelName = ModelName,
                ModelNumber = ModelNumber,
                FriendlyName = Name,
                ManufacturerUrl = ManufacturerUrl,
                ModelUrl = ModelUrl,
                ModelDescription = ModelDescription,
                SerialNumber = SerialNumber
            };
        }
    }
}
