using System.IO;
using Emby.Server.Implementations;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Serialization;
using Jellyfin.DeviceProfiles;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Profiles.Tests
{
    public static class ProfileTestingHelper
    {
        public const string SpecificProfile = "Hisense - Specific";
        public const string SpecificProfile2 = "Hisense - Specific 2";
        public const string GeneralProfile = "Hisense - General";
        public const string GeneralProfile2 = "Hisense - General 2";
        public const string ExactProfile = "Hisense - Exact match";

        private static readonly string _testPathRoot = Path.Combine(Path.GetTempPath(), "jellyfin-test-data");

        public static IDeviceProfileManager CreateProfileManager(bool addData)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
            };

            var appPaths = GetApplicationPaths();
            var pm = new DeviceProfileManager(
                new MyXmlSerializer(),
                appPaths,
                new NullLogger<DeviceProfileManager>(),
                new ManagedFileSystem(new NullLogger<ManagedFileSystem>(), appPaths),
                GetConfigurationManager(conf));

            if (addData)
            {
                CreateDeviceProfileTemplates(pm);
            }

            return pm;
        }

        /// <summary>
        /// Creates test profiles.
        /// </summary>
        /// <param name="manager">The <see cref="IDeviceProfileManager"/> instance.</param>
        private static void CreateDeviceProfileTemplates(IDeviceProfileManager manager)
        {
            manager.AddProfile(
                new DeviceProfile
                {
                    Name = GeneralProfile,
                    Identification = new ()
                    {
                        FriendlyName = "Hisense*",
                        ModelNumber = "123",
                        ModelDescription = "1bc"
                    }
                },
                false);

            manager.AddProfile(
                new DeviceProfile
                {
                    Name = GeneralProfile2,
                    Identification = new ()
                    {
                        FriendlyName = "Hisense*",
                        ModelNumber = "123"
                    }
                },
                false);

            manager.AddProfile(
                new DeviceProfile
                {
                    Name = SpecificProfile2,
                    Identification = new ()
                    {
                        FriendlyName = "Hisense",
                        ModelNumber = "123"
                    }
                },
                false);

            manager.AddProfile(
                new DeviceProfile
                {
                    Name = SpecificProfile,
                    Identification = new ()
                    {
                        Address = "192.168.0.1",
                        FriendlyName = "Hisense*",
                        ModelNumber = "123",
                        ModelDescription = "1bc"
                    },
                    MaxStreamingBitrate = 1000
                },
                false);
        }

        private static IApplicationPaths GetApplicationPaths()
        {
            Directory.CreateDirectory(Path.Combine(_testPathRoot, "config"));
            return new ServerApplicationPaths(
                _testPathRoot,
                Path.Combine(_testPathRoot, "logs"),
                Path.Combine(_testPathRoot, "config"),
                Path.Combine(_testPathRoot, "cache"),
                Path.Combine(_testPathRoot, "jellyfin-web"));
        }

        private static IServerConfigurationManager GetConfigurationManager(NetworkConfiguration conf)
        {
            var configManager = new Mock<IServerConfigurationManager>
            {
                CallBase = true
            };

            var common = new ServerConfiguration
            {
                SaveUnknownDeviceProfilesToDisk = false
            };

            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>())).Returns(conf);
            configManager.Setup(x => x.Configuration).Returns(common);
            return (IServerConfigurationManager)configManager.Object;
        }
    }
}
