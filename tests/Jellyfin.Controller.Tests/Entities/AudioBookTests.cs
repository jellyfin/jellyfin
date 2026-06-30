using System;
using System.Reflection;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class AudioBookTests
{
    [Fact]
    public void AudioBook_DoesNotRequireSourceSerialisation()
    {
        // Without this attribute the Data blob round-trips AdditionalParts/PartRunTimeTicks.
        // Re-adding it would silently break multi-part persistence across restarts.
        Assert.Null(typeof(AudioBook).GetCustomAttribute<RequiresSourceSerialisationAttribute>());
    }

    [Fact]
    public void AudioBook_MultiPartFields_SurviveJsonRoundTrip()
    {
        var book = new AudioBook
        {
            Path = "/books/Defiant/01.mp3",
            AdditionalParts = ["/books/Defiant/02.mp3", "/books/Defiant/03.mp3"],
            PartRunTimeTicks = [1000, 2000, 3000]
        };

        var json = JsonSerializer.Serialize(book, JsonDefaults.Options);
        var restored = JsonSerializer.Deserialize<AudioBook>(json, JsonDefaults.Options);

        Assert.NotNull(restored);
        Assert.Equal(book.AdditionalParts, restored!.AdditionalParts);
        Assert.Equal(book.PartRunTimeTicks, restored.PartRunTimeTicks);
    }

    [Fact]
    public void UpdateFromResolvedItem_AppliesChangedAdditionalParts()
    {
        var existing = new AudioBook { AdditionalParts = [] };
        var resolved = new AudioBook { AdditionalParts = ["/p/02.mp3", "/p/03.mp3"] };

        var updateType = existing.UpdateFromResolvedItem(resolved);

        Assert.Equal(["/p/02.mp3", "/p/03.mp3"], existing.AdditionalParts);
        Assert.True(updateType.HasFlag(ItemUpdateType.MetadataImport));
    }

    [Fact]
    public void UpdateFromResolvedItem_UnchangedAdditionalParts_DoesNotFlagImport()
    {
        var existing = new AudioBook { AdditionalParts = ["/p/02.mp3"] };
        var resolved = new AudioBook { AdditionalParts = ["/p/02.mp3"] };

        var updateType = existing.UpdateFromResolvedItem(resolved);

        Assert.False(updateType.HasFlag(ItemUpdateType.MetadataImport));
    }

    [Fact]
    public void GetMediaSources_MultiPart_YieldsOneSourcePerPartWithValidGuidIds()
    {
        SetupBaseItemStatics();

        var book = new AudioBook
        {
            Id = Guid.NewGuid(),
            Path = "/books/Defiant/01.mp3",
            AdditionalParts = ["/books/Defiant/02.mp3", "/books/Defiant/03.mp3"],
            PartRunTimeTicks = [1000, 2000, 3000]
        };

        var sources = book.GetMediaSources(false);

        Assert.Equal(3, sources.Count);
        Assert.Equal(1000, sources[0].RunTimeTicks);
        Assert.Equal("/books/Defiant/02.mp3", sources[1].Path);
        Assert.Equal(2000, sources[1].RunTimeTicks);
        Assert.Equal("/books/Defiant/03.mp3", sources[2].Path);
        Assert.Equal(3000, sources[2].RunTimeTicks);

        // DynamicHlsController calls Guid.Parse on the source id when transcoding, so every id must be a valid Guid.
        foreach (var source in sources)
        {
            Assert.True(Guid.TryParseExact(source.Id, "N", out _));
        }
    }

    [Fact]
    public void GetMediaSources_NoAdditionalParts_FallsBackToBase()
    {
        SetupBaseItemStatics();

        var book = new AudioBook
        {
            Id = Guid.NewGuid(),
            Path = "/books/Single/book.mp3"
        };

        var sources = book.GetMediaSources(false);

        Assert.Single(sources);
    }

    private static void SetupBaseItemStatics()
    {
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetMediaStreams(It.IsAny<Guid>())).Returns(Array.Empty<MediaStream>());
        mediaSourceManager.Setup(x => x.GetMediaAttachments(It.IsAny<Guid>())).Returns(Array.Empty<MediaAttachment>());

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetNewItemId(It.IsAny<string>(), It.IsAny<Type>()))
            .Returns((string key, Type type) => Guid.NewGuid());

        var mediaSegmentManager = new Mock<IMediaSegmentManager>();
        mediaSegmentManager.Setup(x => x.IsTypeSupported(It.IsAny<BaseItem>())).Returns(false);

        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;
        BaseItem.MediaSegmentManager = mediaSegmentManager.Object;
    }
}
