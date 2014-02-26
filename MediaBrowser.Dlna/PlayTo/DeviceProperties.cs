using System.Collections.Generic;

namespace MediaBrowser.Dlna.PlayTo
{
   public class DeviceProperties
    {
        private string _uuid = string.Empty;
        public string UUID
        {
            get
            {
                return _uuid;
            }
            set
            {
                _uuid = value;
            }
        }

        private string _name = "PlayTo 1.0.0.0";
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private string _clientType = "DLNA";
        public string ClientType
        {
            get
            {
                return _clientType;
            }
            set
            {
                _clientType = value;
            }
        }

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(_displayName) ? _name : _displayName;
            }
            set
            {
                _displayName = value;
            }
        }

        private string _modelName = string.Empty;
        public string ModelName
        {
            get
            {
                return _modelName;
            }
            set
            {
                _modelName = value;
            }
        }

        private string _modelNumber = string.Empty;
        public string ModelNumber
        {
            get
            {
                return _modelNumber;
            }
            set
            {
                _modelNumber = value;
            }
        }

        private string _manufacturer = string.Empty;
        public string Manufacturer
        {
            get
            {
                return _manufacturer;
            }
            set
            {
                _manufacturer = value;
            }
        }

        private string _manufacturerUrl = string.Empty;
        public string ManufacturerUrl
        {
            get
            {
                return _manufacturerUrl;
            }
            set
            {
                _manufacturerUrl = value;
            }
        }

        private string _presentationUrl = string.Empty;
        public string PresentationUrl
        {
            get
            {
                return _presentationUrl;
            }
            set
            {
                _presentationUrl = value;
            }
        }

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

        private uIcon _icon;
        public uIcon Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                _icon = value;
            }
        }

        private string _iconUrl;
        public string IconUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_iconUrl) && _icon != null)
                {
                    if (!_icon.Url.StartsWith("/"))
                        _iconUrl = _baseUrl + "/" + _icon.Url;
                    else
                        _iconUrl = _baseUrl + _icon.Url;
                }

                return _iconUrl;
            }
        }

        private readonly List<uService> _services = new List<uService>();
        public List<uService> Services
        {
            get
            {
                return _services;
            }
        }
    }
}
