using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using ModelMediaInfo = MediaBrowser.Model.MediaInfo.MediaInfo;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class AudioFileProberChapterTests
{
    [Fact]
    public async Task BuildMultiPartChaptersAsync_BuildsCumulativeChaptersWithStrippedNames()
    {
        var durations = new Dictionary<string, long>
        {
            ["/b/02 Prologue.mp3"] = 2000,
            ["/b/03 Chapter 1.mp3"] = 3000
        };

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(x => x.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaInfoRequest req, CancellationToken ct) =>
                new ModelMediaInfo { RunTimeTicks = durations[req.MediaSource.Path] });

        var prober = CreateProber(mediaEncoder: mediaEncoder);
        var book = new AudioBook
        {
            Path = "/b/01 Opening.mp3",
            AdditionalParts = ["/b/02 Prologue.mp3", "/b/03 Chapter 1.mp3"]
        };

        var (chapters, total, partTicks) = await prober.BuildMultiPartChaptersAsync(book, 1000, CancellationToken.None);

        Assert.Equal(3, chapters.Count);
        Assert.Equal("Opening", chapters[0].Name);
        Assert.Equal(0, chapters[0].StartPositionTicks);
        Assert.Equal("Prologue", chapters[1].Name);
        Assert.Equal(1000, chapters[1].StartPositionTicks);
        Assert.Equal("Chapter 1", chapters[2].Name);
        Assert.Equal(3000, chapters[2].StartPositionTicks);
        Assert.Equal(6000, total);
        Assert.Equal([1000L, 2000L, 3000L], partTicks);
    }

    [Fact]
    public async Task SaveAudioBookChaptersAsync_PrefersEmbeddedChapters_WhenPresent()
    {
        var chapterManager = new Mock<IChapterManager>();
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(x => x.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelMediaInfo { RunTimeTicks = 1000 });

        var prober = CreateProber(chapterManager: chapterManager, mediaEncoder: mediaEncoder);
        var book = new AudioBook { Path = "/b/01.mp3", AdditionalParts = ["/b/02.mp3"], PartRunTimeTicks = [0, 0] };
        var embedded = new[] { new ChapterInfo { Name = "Embedded", StartPositionTicks = 0 } };

        await prober.SaveAudioBookChaptersAsync(book, new ModelMediaInfo { Chapters = embedded }, CancellationToken.None);

        chapterManager.Verify(x => x.SaveChapters(book, embedded), Times.Once);
    }

    [Fact]
    public async Task SaveAudioBookChaptersAsync_UsesMultiPart_WhenNoEmbeddedChapters()
    {
        var chapterManager = new Mock<IChapterManager>();
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(x => x.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelMediaInfo { RunTimeTicks = 2000 });

        var prober = CreateProber(mediaEncoder: mediaEncoder, chapterManager: chapterManager);
        var book = new AudioBook { Path = "/b/01 Intro.mp3", AdditionalParts = ["/b/02 Part.mp3"] };

        await prober.SaveAudioBookChaptersAsync(book, new ModelMediaInfo { RunTimeTicks = 1000, Chapters = Array.Empty<ChapterInfo>() }, CancellationToken.None);

        chapterManager.Verify(x => x.SaveChapters(book, It.Is<IReadOnlyList<ChapterInfo>>(c => c.Count == 2)), Times.Once);
        Assert.Equal(3000L, book.RunTimeTicks!.Value);
        Assert.Equal([1000L, 2000L], book.PartRunTimeTicks);
    }

    private static AudioFileProber CreateProber(
        Mock<IMediaEncoder>? mediaEncoder = null,
        Mock<ILibraryManager>? libraryManager = null,
        Mock<IChapterManager>? chapterManager = null)
    {
        return new AudioFileProber(
            NullLogger<AudioFileProber>.Instance,
            null!,
            (mediaEncoder ?? new Mock<IMediaEncoder>()).Object,
            (libraryManager ?? new Mock<ILibraryManager>()).Object,
            null!,
            null!,
            null!,
            (chapterManager ?? new Mock<IChapterManager>()).Object,
            null!);
    }
}
