using MediaBrowser.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests
{
    public class GetPathValueTests
    {
        [Theory]
        [InlineData("https://localhost:8096/ScheduledTasks/1234/Triggers", "", 1, "1234")]
        [InlineData("https://localhost:8096/emby/ScheduledTasks/1234/Triggers", "", 1, "1234")]
        [InlineData("https://localhost:8096/mediabrowser/ScheduledTasks/1234/Triggers", "", 1, "1234")]
        [InlineData("https://localhost:8096/jellyfin/2/ScheduledTasks/1234/Triggers", "jellyfin/2", 1, "1234")]
        [InlineData("https://localhost:8096/jellyfin/2/emby/ScheduledTasks/1234/Triggers", "jellyfin/2", 1, "1234")]
        [InlineData("https://localhost:8096/jellyfin/2/mediabrowser/ScheduledTasks/1234/Triggers", "jellyfin/2", 1, "1234")]
        [InlineData("https://localhost:8096/JELLYFIN/2/ScheduledTasks/1234/Triggers", "jellyfin/2", 1, "1234")]
        [InlineData("https://localhost:8096/JELLYFIN/2/Emby/ScheduledTasks/1234/Triggers", "jellyfin/2", 1, "1234")]
        [InlineData("https://localhost:8096/JELLYFIN/2/MediaBrowser/ScheduledTasks/1234/Triggers", "jellyfin/2", 1, "1234")]
        public void GetPathValueTest(string path, string baseUrl, int index, string value)
        {
            var reqMock = Mock.Of<IRequest>(x => x.PathInfo == path);
            var conf = new ServerConfiguration()
            {
                BaseUrl = baseUrl
            };

            var confManagerMock = Mock.Of<IServerConfigurationManager>(x => x.Configuration == conf);

            var service = new BrandingService(
                new NullLogger<BrandingService>(),
                confManagerMock,
                Mock.Of<IHttpResultFactory>())
            {
                Request = reqMock
            };

            Assert.Equal(value, service.GetPathValue(index).ToString());
        }
    }
}
