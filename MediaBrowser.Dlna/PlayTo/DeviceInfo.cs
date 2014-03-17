using System.Collections.Generic;
using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Dlna.PlayTo
{
    public class DeviceInfo
    {
        public DeviceInfo()
        {
            ClientType = "DLNA";
            Name = "Generic Device";
        }

        public string UUID { get; set; }

        public string Name { get; set; }

        public string ClientType { get; set; }

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(_displayName) ? Name : _displayName;
            }
            set
            {
                _displayName = value;
            }
        }

        public string ModelName { get; set; }

        public string ModelNumber { get; set; }

        public string Manufacturer { get; set; }

        public string ManufacturerUrl { get; set; }

        public string PresentationUrl { get; set; }

        private string _baseUrl = string.Empty;
        public string BaseUrl
        {
            get
            {
                return _baseUrl;
            }
            set
            {
                _baseUrl = value;
            }
        }

        public uIcon Icon { get; set; }

        private readonly List<DeviceService> _services = new List<DeviceService>();
        public List<DeviceService> Services
        {
            get
            {
                return _services;
            }
        }

        public DeviceIdentification ToDeviceIdentification()
        {
            return new DeviceIdentification
            {
                Manufacturer = Manufacturer,
                ModelName = ModelName,
                ModelNumber = ModelNumber,
                FriendlyName = Name,
                ManufacturerUrl = ManufacturerUrl
            };
        }
    }
}
