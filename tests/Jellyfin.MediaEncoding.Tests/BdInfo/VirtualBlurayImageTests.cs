using System;
using System.IO;
using DiscUtils.Udf;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.BdInfo;

public sealed class VirtualBlurayImageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly Mock<IBlurayExaminer> _examiner = new();
    private readonly ServerConfiguration _configuration = new();
    private readonly VirtualBlurayImageManager _images;

    public VirtualBlurayImageTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "BDMV", "STREAM"));
        File.WriteAllBytes(Path.Combine(_root, "BDMV", "index.bdmv"), [1, 2, 3]);
        File.WriteAllBytes(Clip, [4, 5, 6, 7]);
        _examiner
            .Setup(examiner => examiner.GetDiscInfo(_root))
            .Returns(new BlurayDiscInfo { Files = [Clip] });
        var configuration = new Mock<IServerConfigurationManager>();
        configuration.SetupGet(manager => manager.Configuration).Returns(_configuration);
        _images = new VirtualBlurayImageManager(
            _examiner.Object,
            configuration.Object,
            _cache,
            NullLogger<VirtualBlurayImageManager>.Instance);
    }

    private string Clip => Path.Combine(_root, "BDMV", "STREAM", "00001.m2ts");

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(1, true, true)]
    [InlineData(4, false, true)]
    [InlineData(4, true, true)]
    public void ApiRepresentation_UsesMainPlaylistAndPreference(
        int clips,
        bool enabled,
        bool expectedIso)
    {
        // Extras do not turn a single-clip main playlist into a multi-clip title.
        File.WriteAllBytes(Path.Combine(_root, "BDMV", "STREAM", "00002.m2ts"), [8]);
        _examiner
            .Setup(examiner => examiner.GetDiscInfo(_root))
            .Returns(new BlurayDiscInfo { Files = new string[clips] });
        _configuration.UseVirtualIsoForSingleClipBluRays = enabled;
        var source = new MediaSourceInfo
        {
            Id = "source",
            Path = _root,
            VideoType = VideoType.BluRay,
            Protocol = MediaProtocol.File,
            Container = "mpegts",
        };
        _images.ApplyTo(source);
        Assert.Equal(expectedIso ? VideoType.Iso : VideoType.BluRay, source.VideoType);
        Assert.Equal(expectedIso ? "iso" : "mpegts", source.Container);
        Assert.Equal("source", source.Id);
        Assert.Equal(_root, source.Path);
    }

    [Fact]
    public void Image_PreservesBytesAndIndependentSeekPositions()
    {
        var image = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        using var first = image.Open();
        using var second = image.Open();
        using var reader = new UdfReader(first);
        using var clip = reader.OpenFile("BDMV\\STREAM\\00001.m2ts", FileMode.Open);
        Assert.Equal(4, clip.ReadByte());
        clip.Position = 3;
        Assert.Equal(7, clip.ReadByte());
        Assert.Equal(0, second.Position);
        Assert.Equal(image.Length, second.Length);
        Assert.False(reader.FileExists("movie.nfo"));
        _configuration.UseVirtualIsoForSingleClipBluRays = true;
        Assert.Same(image, _images.GetImage(_root));
    }

    [Fact]
    public void Cache_RefreshesOnFileMetadataChangeWithoutExpiry()
    {
        var original = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.Same(original, _images.GetImage(_root));
        _examiner.Verify(examiner => examiner.GetDiscInfo(_root), Times.Once);
        File.SetLastWriteTimeUtc(Clip, File.GetLastWriteTimeUtc(Clip).AddSeconds(1));
        var updated = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.NotEqual(original.Tag, updated.Tag);
        Assert.NotSame(original, updated);
        _examiner.Verify(examiner => examiner.GetDiscInfo(_root), Times.Exactly(2));
        using var obsolete = original.Open();
        using var reader = new UdfReader(obsolete);
        using var clip = reader.OpenFile("BDMV\\STREAM\\00001.m2ts", FileMode.Open);
        Assert.Throws<IOException>(() => clip.ReadByte());
    }

    [Fact]
    public void Image_DoesNotExposeSymlinksOutsideTheDisc()
    {
        File.CreateSymbolicLink(
            Path.Combine(_root, "BDMV", "secret"),
            Path.Combine(_root, "movie.nfo"));
        File.WriteAllText(Path.Combine(_root, "movie.nfo"), "outside the disc");
        Assert.Null(_images.GetImage(_root));
    }

    [Fact]
    public void Cache_RefreshesWhenFilesAreAddedAndRemoved()
    {
        var original = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        var extra = Path.Combine(_root, "BDMV", "extra");
        File.WriteAllBytes(extra, [9]);
        var added = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.NotEqual(original.Tag, added.Tag);
        File.Delete(extra);
        Assert.Equal(original.Tag, _images.GetImage(_root)!.Tag);
    }

    public void Dispose()
    {
        _cache.Dispose();
        Directory.Delete(_root, true);
    }
}
