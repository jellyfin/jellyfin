using System.Collections.Generic;

namespace MediaBrowser.Dlna.PlayTo
{
    public class DeviceInfo
    {
        public string UUID { get; set; }

        public string Name { get; set; }

        public string ClientType { get; set; }

        public string DisplayName { get; set; }

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
    }
}
