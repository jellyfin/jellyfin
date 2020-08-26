#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public class DeviceInfo
    {
        private readonly List<DeviceService> _services = new List<DeviceService>();
        private string _baseUrl = string.Empty;

        public DeviceInfo()
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

        public string BaseUrl
        {
            get => _baseUrl;
            set => _baseUrl = value;
        }

        public DeviceIcon Icon { get; set; }

        public List<DeviceService> Services => _services;

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
