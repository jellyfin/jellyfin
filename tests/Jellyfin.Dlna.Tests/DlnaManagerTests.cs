using Emby.Dlna;
using Emby.Dlna.PlayTo;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Dlna.Tests
{
    public class DlnaManagerTests
    {
        private DlnaManager GetManager()
        {
            var xmlSerializer = new Mock<IXmlSerializer>();
            var fileSystem = new Mock<IFileSystem>();
            var appPaths = new Mock<IApplicationPaths>();
            var loggerFactory = new Mock<ILoggerFactory>();
            var appHost = new Mock<IServerApplicationHost>();

            return new DlnaManager(xmlSerializer.Object, fileSystem.Object, appPaths.Object, loggerFactory.Object, appHost.Object);
        }

        [Fact]
        public void IsMatch_GivenMatchingName_ReturnsTrue()
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
                Identification = new()
                {
                    FriendlyName = "My Device",
                    Manufacturer = "LG Electronics",
                    ManufacturerUrl = "http://www.lge.com",
                    ModelDescription = "LG WebOSTV DMRplus",
                    ModelName = "LG TV",
                    ModelNumber = "1.0",
                }
            };

            var profile2 = new DeviceProfile()
            {
                Name = "Test Profile",
                FriendlyName = "My Device",
                Identification = new DeviceIdentification()
                {
                    FriendlyName = "My Device",
                }
            };

            var deviceMatch = GetManager().IsMatch(device.ToDeviceIdentification(), profile2.Identification);
            var deviceMatch2 = GetManager().IsMatch(device.ToDeviceIdentification(), profile.Identification);

            Assert.True(deviceMatch);
            Assert.True(deviceMatch2);
        }

        [Fact]
        public void IsMatch_GivenNamesAndManufacturersDoNotMatch_ReturnsFalse()
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
                Identification = new()
                {
                    FriendlyName = "My Device",
                    Manufacturer = "LG Electronics",
                    ManufacturerUrl = "http://www.lge.com",
                    ModelDescription = "LG WebOSTV DMRplus",
                    ModelName = "LG TV",
                    ModelNumber = "1.0",
                }
            };

            var deviceMatch = GetManager().IsMatch(device.ToDeviceIdentification(), profile.Identification);

            Assert.False(deviceMatch);
        }

        [Fact]
        public void IsMatch_GivenNamesAndRegExMatch_ReturnsTrue()
        {
            var device = new DeviceInfo()
            {
                Name = "My Device"
            };

            var profile = new DeviceProfile()
            {
                Name = "Test Profile",
                FriendlyName = "My .*",
                Identification = new()
            };

            var deviceMatch = GetManager().IsMatch(device.ToDeviceIdentification(), profile.Identification);

            Assert.True(deviceMatch);
        }
    }
}
