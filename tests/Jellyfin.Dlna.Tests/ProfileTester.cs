using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo;
using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Dlna.Tests
{
    public class ProfileTester
    {
        private bool IsMatch(DeviceIdentification deviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrEmpty(profileInfo.FriendlyName))
            {
                if (deviceInfo.FriendlyName == null || !IsRegexOrSubstringMatch(deviceInfo.FriendlyName, profileInfo.FriendlyName))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.Manufacturer))
            {
                if (deviceInfo.Manufacturer == null || !IsRegexOrSubstringMatch(deviceInfo.Manufacturer, profileInfo.Manufacturer))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ManufacturerUrl))
            {
                if (deviceInfo.ManufacturerUrl == null || !IsRegexOrSubstringMatch(deviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelDescription))
            {
                if (deviceInfo.ModelDescription == null || !IsRegexOrSubstringMatch(deviceInfo.ModelDescription, profileInfo.ModelDescription))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelName))
            {
                if (deviceInfo.ModelName == null || !IsRegexOrSubstringMatch(deviceInfo.ModelName, profileInfo.ModelName))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelNumber))
            {
                if (deviceInfo.ModelNumber == null || !IsRegexOrSubstringMatch(deviceInfo.ModelNumber, profileInfo.ModelNumber))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelUrl))
            {
                if (deviceInfo.ModelUrl == null || !IsRegexOrSubstringMatch(deviceInfo.ModelUrl, profileInfo.ModelUrl))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.SerialNumber))
            {
                if (deviceInfo.SerialNumber == null || !IsRegexOrSubstringMatch(deviceInfo.SerialNumber, profileInfo.SerialNumber))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsRegexOrSubstringMatch(string input, string pattern)
        {
            return input.Contains(pattern, StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        [Fact]
        public void Test_Profile_Matches()
        {
            var source = new DeviceInfo()
            {
                Name = "HelloWorld"
            };

            var dest = new DeviceProfile()
            {
                Name = "Test Subject 1",
                FriendlyName = "HelloWorld",
                Manufacturer = "LG Electronics",
                ManufacturerUrl = "http://www.lge.com",
                ModelDescription = "LG WebOSTV DMRplus",
                ModelName = "LG TV",
                ModelNumber = "1.0",
                Identification = new DeviceIdentification()
                {
                    FriendlyName = "HelloWorld",
                    Manufacturer = "LG Electronics",
                    ManufacturerUrl = "http://www.lge.com",
                    ModelDescription = "LG WebOSTV DMRplus",
                    ModelName = "LG TV",
                    ModelNumber = "1.0",
                }
            };

            Assert.True(IsMatch(dest.Identification, source.ToDeviceIdentification()));
        }
    }
}
