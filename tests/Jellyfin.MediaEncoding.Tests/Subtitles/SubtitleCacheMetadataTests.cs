using System;
using System.Globalization;
using System.IO;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests;

public sealed class SubtitleCacheMetadataTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("jellyfin-subtitle-metadata-").FullName;
    private readonly SubtitleEncoder _encoder;
    private readonly string _cachePath;
    private readonly string _sourcePath;

    public SubtitleCacheMetadataTests()
    {
        _cachePath = Path.Combine(_directory, "3.srt");
        _sourcePath = Path.Combine(_directory, "movie.mkv");
        File.WriteAllText(_cachePath, "cached subtitle");
        File.WriteAllText(_sourcePath, "source");
        var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
        fixture.Freeze<Mock<IFileSystem>>().Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) =>
        {
            var info = new FileInfo(path);
            return new FileSystemMetadata
            {
                Exists = info.Exists,
                Length = info.Exists ? info.Length : 0,
                LastWriteTimeUtc = info.LastWriteTimeUtc
            };
        });
        _encoder = fixture.Create<SubtitleEncoder>();
    }

    [Fact]
    public void Cache_LogicalIndexReassignedToDifferentTrack_IsRejected()
    {
        var source = CreateSource();
        var french = source.MediaStreams[3];
        _encoder.WriteCacheMeta(_cachePath, source, french);

        AddSidecar(source);
        var english = source.MediaStreams[3];
        Assert.Equal(3, english.Index);
        Assert.False(_encoder.IsCachedSubtitleFresh(_cachePath, source, english));
        Assert.True(_encoder.IsCachedSubtitleFresh(_cachePath, source, french));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cache_SourcePathChangesWithSameRevision_IsRejected(bool remote)
    {
        var source = CreateSource();
        if (remote)
        {
            source.Path = "https://example.invalid/a.mkv";
        }

        var subtitle = source.MediaStreams[3];
        _encoder.WriteCacheMeta(_cachePath, source, subtitle);
        Assert.True(_encoder.IsCachedSubtitleFresh(_cachePath, source, subtitle));
        var replacement = Path.Combine(_directory, "other.mkv");
        File.Copy(_sourcePath, replacement);
        File.SetLastWriteTimeUtc(replacement, File.GetLastWriteTimeUtc(_sourcePath));
        source.Path = remote ? "https://example.invalid/b.mkv" : replacement;
        Assert.False(_encoder.IsCachedSubtitleFresh(_cachePath, source, subtitle));
    }

    [Fact]
    public void Cache_SourceRevisionChanges_IsRejected()
    {
        var source = CreateSource();
        var subtitle = source.MediaStreams[3];
        _encoder.WriteCacheMeta(_cachePath, source, subtitle);
        File.AppendAllText(_sourcePath, "replacement");
        Assert.False(_encoder.IsCachedSubtitleFresh(_cachePath, source, subtitle));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cache_LegacyOrMissingMetadata_IsRejected(bool legacy)
    {
        var source = CreateSource();
        if (legacy)
        {
            var info = new FileInfo(_sourcePath);
            File.WriteAllText(_cachePath + ".meta", string.Create(CultureInfo.InvariantCulture, $"{info.Length}:{info.LastWriteTimeUtc.Ticks}"));
        }

        Assert.False(_encoder.IsCachedSubtitleFresh(_cachePath, source, source.MediaStreams[3]));
    }

    private MediaSourceInfo CreateSource() => new()
    {
        Path = _sourcePath,
        MediaStreams =
        [
            new() { Index = 0, Type = MediaStreamType.Video },
            new() { Index = 1, Type = MediaStreamType.Audio },
            new() { Index = 2, Type = MediaStreamType.Subtitle, Language = "eng" },
            new() { Index = 3, Type = MediaStreamType.Subtitle, Language = "fra" }
        ]
    };

    private static void AddSidecar(MediaSourceInfo source)
    {
        source.MediaStreams = [new MediaStream { IsExternal = true, Type = MediaStreamType.Subtitle, Path = "sidecar.srt" }, .. source.MediaStreams];
        for (var i = 0; i < source.MediaStreams.Count; i++)
        {
            source.MediaStreams[i].Index = i;
        }
    }

    public void Dispose()
    {
        _encoder.Dispose();
        Directory.Delete(_directory, true);
    }
}
