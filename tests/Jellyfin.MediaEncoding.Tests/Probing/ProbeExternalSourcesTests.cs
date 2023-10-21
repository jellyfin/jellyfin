using System.Collections.Generic;
using System.Threading;
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
        public void GetMediaInfo_Uses_UserAgent()
        {
            var encoder = new MediaEncoder(
                Mock.Of<ILogger<MediaEncoder>>(),
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IBlurayExaminer>(),
                Mock.Of<ILocalizationManager>(),
                new ConfigurationBuilder().Build(),
                Mock.Of<IServerConfigurationManager>());

            var req = new MediaBrowser.Controller.MediaEncoding.MediaInfoRequest()
            {
                MediaSource = new MediaBrowser.Model.Dto.MediaSourceInfo
                {
                    Path = "/path/to/stream",
                    Protocol = MediaProtocol.Http,
                    RequiredHttpHeaders = new Dictionary<string, string>()
                    {
                        { "user_agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/530.35 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/530.35" },
                    }
                },
                ExtractChapters = false,
                MediaType = MediaBrowser.Model.Dlna.DlnaProfileType.Video,
            };

            encoder.GetMediaInfo(req, CancellationToken.None);
        }
    }
}
