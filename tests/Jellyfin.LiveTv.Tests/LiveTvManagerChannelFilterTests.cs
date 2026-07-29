using System;
using System.IO;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.LiveTv.Guide;
using Jellyfin.LiveTv.Timers;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class LiveTvManagerChannelFilterTests
    {
        [Fact]
        public void GetInternalChannels_HideChannelsWithoutProgrammes_AppliesGuideDataTagFilter()
        {
            InternalItemsQuery? captured = null;
            var manager = CreateManager(
                new LiveTvOptions { HideChannelsWithoutProgrammes = true },
                q => captured = q);

            manager.GetInternalChannels(new LiveTvChannelQuery(), new DtoOptions(), CancellationToken.None);

            Assert.NotNull(captured);
            Assert.NotNull(captured.Tags);
            Assert.Contains(GuideManager.GuideDataTagName, captured.Tags);
        }

        [Fact]
        public void GetInternalChannels_ShowAllChannels_DoesNotFilterOnGuideDataTag()
        {
            InternalItemsQuery? captured = null;
            var manager = CreateManager(
                new LiveTvOptions { HideChannelsWithoutProgrammes = false },
                q => captured = q);

            manager.GetInternalChannels(new LiveTvChannelQuery(), new DtoOptions(), CancellationToken.None);

            Assert.NotNull(captured);
            Assert.True(captured.Tags is null || captured.Tags.Length == 0);
        }

        private static LiveTvManager CreateManager(LiveTvOptions options, Action<InternalItemsQuery> onQuery)
        {
            var libraryManager = new Mock<ILibraryManager>();
            libraryManager
                .Setup(x => x.GetNamedView(It.IsAny<string>(), It.IsAny<CollectionType>(), It.IsAny<string>()))
                .Returns(new UserView { Id = Guid.NewGuid() });
            libraryManager
                .Setup(x => x.GetItemsResult(It.IsAny<InternalItemsQuery>()))
                .Callback<InternalItemsQuery>(q => onQuery(q))
                .Returns(new QueryResult<BaseItem>());

            var config = new Mock<IServerConfigurationManager>();
            config.Setup(x => x.GetConfiguration("livetv")).Returns(options);

            var localization = new Mock<ILocalizationManager>();
            localization.Setup(x => x.GetLocalizedString(It.IsAny<string>())).Returns("Live TV");

            return new LiveTvManager(
                config.Object,
                NullLogger<LiveTvManager>.Instance,
                Mock.Of<IUserDataManager>(),
                Mock.Of<IDtoService>(),
                Mock.Of<IUserManager>(),
                libraryManager.Object,
                localization.Object,
                Mock.Of<IChannelManager>(),
                Mock.Of<IRecordingsManager>(),
                CreateDtoService(),
                new ILiveTvService[] { CreateDefaultService() });
        }

        private static LiveTvDtoService CreateDtoService()
            => new LiveTvDtoService(
                Mock.Of<IDtoService>(),
                Mock.Of<IImageProcessor>(),
                NullLogger<LiveTvDtoService>.Instance,
                Mock.Of<IApplicationHost>(),
                Mock.Of<ILibraryManager>());

        private static DefaultLiveTvService CreateDefaultService()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "jf-livetv-test-" + Guid.NewGuid().ToString("N"));

            var appPaths = new Mock<IApplicationPaths>();
            appPaths.Setup(x => x.DataPath).Returns(tempDir);

            var configManager = new Mock<IConfigurationManager>();
            configManager.Setup(x => x.CommonApplicationPaths).Returns(appPaths.Object);

            var timerManager = new TimerManager(NullLogger<TimerManager>.Instance, configManager.Object);
            var seriesTimerManager = new SeriesTimerManager(NullLogger<SeriesTimerManager>.Instance, configManager.Object);

            return new DefaultLiveTvService(
                NullLogger<DefaultLiveTvService>.Instance,
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<ITunerHostManager>(),
                Mock.Of<IListingsManager>(),
                Mock.Of<IRecordingsManager>(),
                Mock.Of<ILibraryManager>(),
                CreateDtoService(),
                timerManager,
                seriesTimerManager);
        }
    }
}
