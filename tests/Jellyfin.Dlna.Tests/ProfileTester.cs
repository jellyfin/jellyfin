using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.Dlna;
using Emby.Dlna.PlayTo;
using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Dlna.Tests
{
    public class ProfileTester
    {
        [Fact]
        public void Test_Profile_Matches()
        {
            var device = new DeviceInfo()
            {
                Name = "My Device",
                Manufacturer = "LG Electronics",
                ManufacturerUrl = "http://www.lge.com",
                ModelDescription = "LG WebOSTV DMRplus",
                ModelName = "LG TV",
                ModelNumber = "1.0",
            };

            var profile = new DeviceProfile()
            {
                Name = "Test Profile",
                FriendlyName = "My Device",
                Manufacturer = "LG Electronics",
                ManufacturerUrl = "http://www.lge.com",
                ModelDescription = "LG WebOSTV DMRplus",
                ModelName = "LG TV",
                ModelNumber = "1.0",
                Identification = new DeviceIdentification()
                {
                    FriendlyName = "My Device",
                    Manufacturer = "LG Electronics",
                    ManufacturerUrl = "http://www.lge.com",
                    ModelDescription = "LG WebOSTV DMRplus",
                    ModelName = "LG TV",
                    ModelNumber = "1.0",
                }
            };

            Assert.True(DlnaManager.IsMatch(device.ToDeviceIdentification(), profile.Identification));

            var profile2 = new DeviceProfile()
            {
                Name = "Test Profile",
                FriendlyName = "My Device",
                Identification = new DeviceIdentification()
                {
                    FriendlyName = "My Device",
                }
            };

            Assert.True(DlnaManager.IsMatch(device.ToDeviceIdentification(), profile2.Identification));
        }

        [Fact]
        public void Test_Profile_NoMatch()
        {
            var device = new DeviceInfo()
            {
                Name = "My Device",
                Manufacturer = "JVC"
            };

            var profile = new DeviceProfile()
            {
                Name = "Test Profile",
                FriendlyName = "My Device",
                Manufacturer = "LG Electronics",
                ManufacturerUrl = "http://www.lge.com",
                ModelDescription = "LG WebOSTV DMRplus",
                ModelName = "LG TV",
                ModelNumber = "1.0",
                Identification = new DeviceIdentification()
                {
                    FriendlyName = "My Device",
                    Manufacturer = "LG Electronics",
                    ManufacturerUrl = "http://www.lge.com",
                    ModelDescription = "LG WebOSTV DMRplus",
                    ModelName = "LG TV",
                    ModelNumber = "1.0",
                }
            };

            Assert.False(DlnaManager.IsMatch(device.ToDeviceIdentification(), profile.Identification));
        }
    }
}
