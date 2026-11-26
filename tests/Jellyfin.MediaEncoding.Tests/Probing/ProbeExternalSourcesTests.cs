using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing
{
    public class ProbeExternalSourcesTests
    {
        [Fact]
        public void GetExtraArguments_Forwards_UserAgent()
        {
            var encoder = new MediaEncoder(
                Mock.Of<ILogger<MediaEncoder>>(),
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IBlurayExaminer>(),
                Mock.Of<ILocalizationManager>(),
                new ConfigurationBuilder().Build(),
                Mock.Of<IServerConfigurationManager>());

            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            var req = new MediaBrowser.Controller.MediaEncoding.MediaInfoRequest()
            {
                MediaSource = new MediaBrowser.Model.Dto.MediaSourceInfo
                {
                    Path = "/path/to/stream",
                    Protocol = MediaProtocol.Http,
                    RequiredHttpHeaders = new Dictionary<string, string>()
                    {
                        { "User-Agent", userAgent },
                    }
                },
                ExtractChapters = false,
                MediaType = MediaBrowser.Model.Dlna.DlnaProfileType.Video,
            };

            var extraArg = encoder.GetExtraArguments(req);

            Assert.Contains($"-user_agent \"{userAgent}\"", extraArg, StringComparison.InvariantCulture);
        }

        [Fact]
        public void GetExtraArguments_UsesSharedProbeSize()
        {
            var extraArg = GetExtraArguments(new Dictionary<string, string?>
            {
                { MediaBrowser.Controller.Extensions.ConfigurationExtensions.FfmpegProbeSizeKey, "1G" }
            });

            Assert.Contains("-probesize 1G", extraArg, StringComparison.InvariantCulture);
        }

        [Fact]
        public void GetExtraArguments_IgnoresPlaybackProbeSize()
        {
            // Scanning must keep using the shared key; the playback override applies only to playback.
            var extraArg = GetExtraArguments(new Dictionary<string, string?>
            {
                { MediaBrowser.Controller.Extensions.ConfigurationExtensions.FfmpegProbeSizeKey, "1G" },
                { MediaBrowser.Controller.Extensions.ConfigurationExtensions.FfmpegPlaybackProbeSizeKey, "50M" }
            });

            Assert.Contains("-probesize 1G", extraArg, StringComparison.InvariantCulture);
            Assert.DoesNotContain("-probesize 50M", extraArg, StringComparison.InvariantCulture);
        }

        private static string GetExtraArguments(Dictionary<string, string?> settings)
        {
            var encoder = new MediaEncoder(
                Mock.Of<ILogger<MediaEncoder>>(),
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IBlurayExaminer>(),
                Mock.Of<ILocalizationManager>(),
                new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
                Mock.Of<IServerConfigurationManager>());

            var req = new MediaBrowser.Controller.MediaEncoding.MediaInfoRequest()
            {
                MediaSource = new MediaBrowser.Model.Dto.MediaSourceInfo
                {
                    Path = "/path/to/file.mkv",
                    Protocol = MediaProtocol.File,
                    RequiredHttpHeaders = new Dictionary<string, string>()
                },
                ExtractChapters = false,
                MediaType = MediaBrowser.Model.Dlna.DlnaProfileType.Video,
            };

            return encoder.GetExtraArguments(req);
        }
    }
}
