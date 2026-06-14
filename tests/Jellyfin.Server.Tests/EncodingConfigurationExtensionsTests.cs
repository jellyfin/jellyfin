using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Server.Tests
{
    public class EncodingConfigurationExtensionsTests
    {
        private static Mock<IConfigurationManager> CreateConfig(EncodingOptions options, Mock<IApplicationPaths> paths)
        {
            var config = new Mock<IConfigurationManager>();
            config.Setup(c => c.GetConfiguration("encoding")).Returns(options);
            config.SetupGet(c => c.CommonApplicationPaths).Returns(paths.Object);
            return config;
        }

        [Fact]
        public void GetTranscodePath_UnwritableConfiguredPath_FallsBackToDefaultWithoutChangingConfig()
        {
            const string CachePath = "/cache";
            var defaultPath = Path.Combine(CachePath, "transcodes");
            const string BadPath = "/var/empty/not-writable-transcode";
            var options = new EncodingOptions { TranscodingTempPath = BadPath };

            var paths = new Mock<IApplicationPaths>();
            paths.SetupGet(p => p.CachePath).Returns(CachePath);
            paths.Setup(p => p.CreateAndCheckMarker(BadPath, "transcode", true))
                .Throws(new UnauthorizedAccessException("denied"));

            var config = CreateConfig(options, paths);

            var result = config.Object.GetTranscodePath();

            // Falls back to the default, and leaves the configured value untouched so a transient cause
            // (e.g. a not-yet-mounted drive) self-heals on a later call.
            Assert.Equal(defaultPath, result);
            Assert.Equal(BadPath, options.TranscodingTempPath);
            paths.Verify(p => p.CreateAndCheckMarker(defaultPath, "transcode", true), Times.Once);
        }

        [Fact]
        public void GetTranscodePath_WritableConfiguredPath_ReturnsConfiguredPath()
        {
            const string GoodPath = "/mnt/fast/transcode";
            var options = new EncodingOptions { TranscodingTempPath = GoodPath };

            var paths = new Mock<IApplicationPaths>();
            paths.SetupGet(p => p.CachePath).Returns("/cache");

            var config = CreateConfig(options, paths);

            Assert.Equal(GoodPath, config.Object.GetTranscodePath());
        }

        [Fact]
        public void GetTranscodePath_NoConfiguredPath_ReturnsDefault()
        {
            const string CachePath = "/cache";
            var options = new EncodingOptions { TranscodingTempPath = null };

            var paths = new Mock<IApplicationPaths>();
            paths.SetupGet(p => p.CachePath).Returns(CachePath);

            var config = CreateConfig(options, paths);

            Assert.Equal(Path.Combine(CachePath, "transcodes"), config.Object.GetTranscodePath());
        }
    }
}
