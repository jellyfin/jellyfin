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
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings;

public sealed class RecordingsMetadataManagerTests
{
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
            libraryManager.Object);

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
}
