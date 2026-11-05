using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Extensions;
using Jellyfin.LiveTv.Recordings;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings;

public sealed class RecordingsMetadataManagerTests
{
    private const string ImageUrl = "https://json.schedulesdirect.org/20141201/image/assets/p1_b.jpg?token=abc";

    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "jellyfin-test-" + Guid.NewGuid());

    [Fact]
    public async Task SaveRecordingMetadata_DateAddedIsUtc()
    {
        Directory.CreateDirectory(_tempDir);
        var recordingPath = Path.Combine(_tempDir, "test-recording.ts");
        FileHelper.CreateEmpty(recordingPath);

        var config = new Mock<IConfigurationManager>();
        config.Setup(c => c.GetConfiguration("livetv"))
            .Returns(new LiveTvOptions { SaveRecordingNFO = true, SaveRecordingImages = false });
        config.Setup(c => c.GetConfiguration("xbmcmetadata"))
            .Returns(new XbmcMetadataOptions());

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(Array.Empty<BaseItem>());

        var manager = new RecordingsMetadataManager(
            NullLogger<RecordingsMetadataManager>.Instance,
            config.Object,
            libraryManager.Object,
            Mock.Of<ISchedulesDirectService>());

        var timer = new TimerInfo { Name = "Test Recording", ProgramId = null };

        var beforeUtc = DateTime.UtcNow.AddSeconds(-2);
        await manager.SaveRecordingMetadata(timer, recordingPath, null);
        var afterUtc = DateTime.UtcNow.AddSeconds(2);

        var doc = new XmlDocument();
        doc.Load(Path.ChangeExtension(recordingPath, ".nfo"));
        var dateAddedText = doc.SelectSingleNode("//dateadded")?.InnerText ?? string.Empty;
        var parsed = DateTime.ParseExact(
            dateAddedText,
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture);

        Assert.InRange(parsed, beforeUtc, afterUtc);
    }

    [Fact]
    public async Task SaveRecordingMetadata_RemoteImage_GoesThroughSchedulesDirect()
    {
        // Schedules Direct counts every image request, so a recording must not fetch one behind its back.
        var (manager, libraryManager, schedulesDirect, recordingPath) = CreateImageManager();

        await manager.SaveRecordingMetadata(new TimerInfo { Name = "Test Recording", ProgramId = "p1" }, recordingPath, null);

        schedulesDirect.Verify(s => s.ConvertImageToLocal(It.IsAny<BaseItem>(), It.Is<ItemImageInfo>(i => i.Path == ImageUrl), 0), Times.Once);
        libraryManager.Verify(
            l => l.ConvertImageToLocal(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    private (RecordingsMetadataManager Manager, Mock<ILibraryManager> LibraryManager, Mock<ISchedulesDirectService> SchedulesDirect, string RecordingPath) CreateImageManager()
    {
        Directory.CreateDirectory(_tempDir);
        var recordingPath = Path.Combine(_tempDir, "test-recording.ts");

        var config = new Mock<IConfigurationManager>();
        config.Setup(c => c.GetConfiguration("livetv"))
            .Returns(new LiveTvOptions { SaveRecordingNFO = false, SaveRecordingImages = true });

        var program = new LiveTvProgram
        {
            Name = "Test",
            ImageInfos = [new ItemImageInfo { Path = ImageUrl, Type = ImageType.Primary }]
        };
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([program]);

        var schedulesDirect = new Mock<ISchedulesDirectService>();
        var manager = new RecordingsMetadataManager(
            NullLogger<RecordingsMetadataManager>.Instance,
            config.Object,
            libraryManager.Object,
            schedulesDirect.Object);

        return (manager, libraryManager, schedulesDirect, recordingPath);
    }
}
