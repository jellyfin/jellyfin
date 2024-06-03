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
    }
}
