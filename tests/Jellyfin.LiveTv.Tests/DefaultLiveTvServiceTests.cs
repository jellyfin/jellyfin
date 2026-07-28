using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Timers;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class DefaultLiveTvServiceTests
    {
        private static DefaultLiveTvService CreateService(ITunerHostManager tunerHostManager)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "jf-livetv-test-" + Guid.NewGuid().ToString("N"));

            var appPaths = new Mock<IApplicationPaths>();
            appPaths.Setup(x => x.DataPath).Returns(tempDir);

            var configManager = new Mock<IConfigurationManager>();
            configManager.Setup(x => x.CommonApplicationPaths).Returns(appPaths.Object);

            var timerManager = new TimerManager(NullLogger<TimerManager>.Instance, configManager.Object);
            var seriesTimerManager = new SeriesTimerManager(NullLogger<SeriesTimerManager>.Instance, configManager.Object);

            var dtoService = new LiveTvDtoService(
                Mock.Of<IDtoService>(),
                Mock.Of<IImageProcessor>(),
                NullLogger<LiveTvDtoService>.Instance,
                Mock.Of<IApplicationHost>(),
                Mock.Of<ILibraryManager>());

            return new DefaultLiveTvService(
                NullLogger<DefaultLiveTvService>.Instance,
                Mock.Of<IServerConfigurationManager>(),
                tunerHostManager,
                Mock.Of<IListingsManager>(),
                Mock.Of<IRecordingsManager>(),
                Mock.Of<ILibraryManager>(),
                dtoService,
                timerManager,
                seriesTimerManager);
        }

        [Fact]
        public async Task GetProgramsAsync_UnmatchedChannelId_ReturnsEmpty()
        {
            var tunerHostManager = new Mock<ITunerHostManager>();
            tunerHostManager.Setup(x => x.TunerHosts).Returns(Array.Empty<ITunerHost>());

            var service = CreateService(tunerHostManager.Object);

            var result = await service.GetProgramsAsync(
                "channel-that-does-not-exist",
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                CancellationToken.None);

            Assert.Empty(result);
        }
    }
}
