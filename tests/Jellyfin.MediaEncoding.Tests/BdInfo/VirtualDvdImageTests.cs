using System;
using System.IO;
using DiscUtils.Udf;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.BdInfo;

public sealed class VirtualDvdImageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Image_PreservesDvdFilesAndUsesSameCacheForBothPaths(bool useVideoTsPath)
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var videoTs = Directory.CreateDirectory(Path.Combine(root.FullName, "VIDEO_TS"));
            byte[] payload = [1, 2, 3, 4];
            File.WriteAllBytes(Path.Combine(videoTs.FullName, "VIDEO_TS.IFO"), payload);
            File.WriteAllBytes(Path.Combine(videoTs.FullName, "VTS_01_1.VOB"), payload);
            File.WriteAllText(Path.Combine(root.FullName, "movie.nfo"), "Not part of the DVD");
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var images = new VirtualBlurayImageManager(
                cache,
                NullLogger<VirtualBlurayImageManager>.Instance,
                _ => false);
            var source = new MediaSourceInfo
            {
                Path = useVideoTsPath ? videoTs.FullName : root.FullName,
                Protocol = MediaProtocol.File,
                VideoType = VideoType.Dvd,
                Container = "dvd",
            };
            images.ApplyTo(source);
            Assert.Equal(VideoType.Iso, source.VideoType);
            Assert.Equal(IsoType.Dvd, source.IsoType);
            Assert.Equal("iso", source.Container);
            var image = images.GetImage(root.FullName)!;
            Assert.Same(image, images.GetImage(videoTs.FullName));
            Assert.Equal(image.Length, source.Size);
            Assert.Equal(image.Tag, source.ETag);
            using var stream = image.Open();
            using var reader = new UdfReader(stream);
            Assert.False(reader.FileExists("movie.nfo"));
            using var file = reader.OpenFile(
                "VIDEO_TS/VTS_01_1.VOB",
                FileMode.Open,
                FileAccess.Read);
            var actual = new byte[payload.Length];
            file.ReadExactly(actual);
            Assert.Equal(payload, actual);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Image_RequiresDvdIndex()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "VIDEO_TS"));
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var images = new VirtualBlurayImageManager(
                cache,
                NullLogger<VirtualBlurayImageManager>.Instance,
                _ => false);
            Assert.Null(images.GetImage(root.FullName));
        }
        finally
        {
            root.Delete(true);
        }
    }
}
