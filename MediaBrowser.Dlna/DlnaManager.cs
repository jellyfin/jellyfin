using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Dlna.Profiles;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Dlna
{
    public class DlnaManager : IDlnaManager
    {
        private IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        public DlnaManager(IXmlSerializer xmlSerializer, IFileSystem fileSystem, IJsonSerializer jsonSerializer)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;

            GetProfiles();
        }

        public IEnumerable<DeviceProfile> GetProfiles()
        {
            var list = new List<DeviceProfile>
            {
                new SamsungSmartTvProfile(),
                new Xbox360Profile(),
                new XboxOneProfile(),
                new SonyPs3Profile(),
                new SonyBravia2010Profile(),
                new SonyBravia2011Profile(),
                new SonyBravia2012Profile(),
                new SonyBravia2013Profile(),
                new SonyBlurayPlayer2013Profile(),
                new SonyBlurayPlayerProfile(),
                new PanasonicVieraProfile(),
                new WdtvLiveProfile(),
                new DenonAvrProfile(),
                new LinksysDMA2100Profile(),
                new LgTvProfile()
            };

            foreach (var item in list)
            {
                //_xmlSerializer.SerializeToFile(item, "d:\\" + _fileSystem.GetValidFilename(item.Name) + ".xml");
                //_jsonSerializer.SerializeToFile(item, "d:\\" + _fileSystem.GetValidFilename(item.Name) + ".json");
            }

            return list;
        }

        public DeviceProfile GetDefaultProfile()
        {
            return new DefaultProfile();
        }

        public DeviceProfile GetProfile(DeviceIdentification deviceInfo)
        {
            return GetProfiles().FirstOrDefault(i => IsMatch(deviceInfo, i.Identification)) ??
                GetDefaultProfile();
        }

        private bool IsMatch(DeviceIdentification deviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrWhiteSpace(profileInfo.DeviceDescription))
            {
                if (deviceInfo.DeviceDescription == null || !Regex.IsMatch(deviceInfo.DeviceDescription, profileInfo.DeviceDescription))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.FriendlyName))
            {
                if (deviceInfo.FriendlyName == null || !Regex.IsMatch(deviceInfo.FriendlyName, profileInfo.FriendlyName))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.Manufacturer))
            {
                if (deviceInfo.Manufacturer == null || !Regex.IsMatch(deviceInfo.Manufacturer, profileInfo.Manufacturer))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ManufacturerUrl))
            {
                if (deviceInfo.ManufacturerUrl == null || !Regex.IsMatch(deviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelDescription))
            {
                if (deviceInfo.ModelDescription == null || !Regex.IsMatch(deviceInfo.ModelDescription, profileInfo.ModelDescription))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelName))
            {
                if (deviceInfo.ModelName == null || !Regex.IsMatch(deviceInfo.ModelName, profileInfo.ModelName))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelNumber))
            {
                if (deviceInfo.ModelNumber == null || !Regex.IsMatch(deviceInfo.ModelNumber, profileInfo.ModelNumber))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelUrl))
            {
                if (deviceInfo.ModelUrl == null || !Regex.IsMatch(deviceInfo.ModelUrl, profileInfo.ModelUrl))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.SerialNumber))
            {
                if (deviceInfo.SerialNumber == null || !Regex.IsMatch(deviceInfo.SerialNumber, profileInfo.SerialNumber))
                    return false;
            }

            return true;
        }
    }
}