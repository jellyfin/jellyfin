using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Streams;
using DiscUtils.Udf;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.BdInfo;

public sealed class VirtualBlurayImageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly VirtualBlurayImageManager _images;

    public VirtualBlurayImageTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "BDMV", "STREAM"));
        File.WriteAllBytes(Path.Combine(_root, "BDMV", "index.bdmv"), [1, 2, 3]);
        File.WriteAllBytes(Clip, [4, 5, 6, 7]);
        _images = new VirtualBlurayImageManager(
            _cache,
            NullLogger<VirtualBlurayImageManager>.Instance,
            _ => false);
    }

    private string Clip => Path.Combine(_root, "BDMV", "STREAM", "00001.m2ts");

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void ApiRepresentation_UsesIsoForEveryClipCount(int clips)
    {
        for (var i = 2; i <= clips; i++)
        {
            File.WriteAllBytes(Path.Combine(_root, "BDMV", "STREAM", $"{i:D5}.m2ts"), [8]);
        }

        var source = new MediaSourceInfo
        {
            Id = "source",
            Path = _root,
            VideoType = VideoType.BluRay,
            Protocol = MediaProtocol.File,
            Container = "mpegts",
        };
        _images.ApplyTo(source);
        Assert.Equal(VideoType.Iso, source.VideoType);
        Assert.Equal("iso", source.Container);
        Assert.Equal("source", source.Id);
        Assert.Equal(_root + ".iso", source.Path);
    }

    [Fact]
    public async Task ConcurrentRequests_ReuseImage()
    {
        var images = await Task.WhenAll(
            Enumerable
                .Range(0, 16)
                .Select(i =>
                    Task.Run(() =>
                        _images.GetImage(i % 2 == 0 ? _root : Path.Combine(_root, "BDMV")))));
        Assert.All(images, image => Assert.Same(images[0], image));
    }

    [Fact]
    public async Task ConcurrentMetadataRequests_ScanOnceForBothPathForms()
    {
        var entries = 0;
        using var images = new VirtualBlurayImageManager(
            _cache,
            NullLogger<VirtualBlurayImageManager>.Instance,
            _ =>
            {
                Interlocked.Increment(ref entries);
                return false;
            });
        await Task.WhenAll(
            Enumerable
                .Range(0, 16)
                .Select(i =>
                    Task.Run(() =>
                    {
                        var source = new MediaSourceInfo
                        {
                            Path =
                                i % 2 == 0
                                    ? _root + Path.DirectorySeparatorChar
                                    : Path.Combine(_root, "BDMV"),
                            VideoType = VideoType.BluRay,
                            Protocol = MediaProtocol.File,
                        };
                        images.ApplyTo(source);
                        Assert.Equal(VideoType.Iso, source.VideoType);
                    })));
        Assert.Equal(3, entries);
    }

    [Fact]
    public void Metadata_ReusesCacheUntilStreamingValidatesChanges()
    {
        MediaSourceInfo Source(string path) =>
            new()
            {
                Path = path,
                VideoType = VideoType.BluRay,
                Protocol = MediaProtocol.File,
            };

        var first = Source(_root);
        _images.ApplyTo(first);
        File.WriteAllBytes(Clip, new byte[8192]);
        var stale = Source(Path.Combine(_root, "BDMV"));
        _images.ApplyTo(stale);
        Assert.Equal(first.ETag, stale.ETag);
        Assert.Equal(first.Size, stale.Size);

        var updated = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.NotEqual(first.ETag, updated.Tag);
        Assert.NotEqual(first.Size, updated.Length);
        var refreshed = Source(_root);
        _images.ApplyTo(refreshed);
        Assert.Equal(updated.Tag, refreshed.ETag);
        Assert.Equal(updated.Length, refreshed.Size);
    }

    [Fact]
    public void Image_DoesNotScanMediaPayload()
    {
        using var lockedPayload = new FileStream(
            Clip,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);
        var source = new MediaSourceInfo
        {
            Path = _root,
            VideoType = VideoType.BluRay,
            Protocol = MediaProtocol.File,
        };
        _images.ApplyTo(source);
        Assert.Equal(VideoType.Iso, source.VideoType);
        using var stream = _images.GetImage(_root)!.Open();
        Assert.True(stream.Length > 0);
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
        Assert.Same(image, _images.GetImage(_root));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Image_AsyncReadPreservesPayloadAndHonorsCancellation(bool useMemory)
    {
        using var image = _images.GetImage(_root)!.Open();
        var payload = ((SparseStream)image).Extents.Single(extent => extent.Length == 4);
        image.Position = payload.Start;
        var bytes = new byte[4];
        var read = useMemory
            ? await image.ReadAsync(bytes.AsMemory(), TestContext.Current.CancellationToken)
            : await image.ReadAsync(bytes, 0, bytes.Length, TestContext.Current.CancellationToken);
        Assert.Equal(4, read);
        Assert.Equal(new byte[] { 4, 5, 6, 7 }, bytes);

        image.Position = payload.Start + 1;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (useMemory)
            {
                _ = await image.ReadAsync(bytes.AsMemory(), cancellation.Token);
            }
            else
            {
                _ = await image.ReadAsync(bytes, 0, bytes.Length, cancellation.Token);
            }
        });
        Assert.Equal(payload.Start + 1, image.Position);
    }

    [Fact]
    public void Cache_RefreshesOnFileMetadataChangeWithoutExpiry()
    {
        var original = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.Same(original, _images.GetImage(_root));
        File.SetLastWriteTimeUtc(Clip, File.GetLastWriteTimeUtc(Clip).AddSeconds(1));
        var updated = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.NotEqual(original.Tag, updated.Tag);
        Assert.NotSame(original, updated);
        using var obsolete = original.Open();
        using var reader = new UdfReader(obsolete);
        using var clip = reader.OpenFile("BDMV\\STREAM\\00001.m2ts", FileMode.Open);
        Assert.Throws<IOException>(() => clip.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Image_FollowsExternalPayloadLinksAndTracksTargetChanges(bool directoryLink)
    {
        var downloads = Directory.CreateTempSubdirectory();
        try
        {
            var target = Path.Combine(downloads.FullName, "00001.m2ts");
            var bytes = Enumerable.Range(0, 8192).Select(i => (byte)i).ToArray();
            File.WriteAllBytes(target, bytes);
            File.SetLastWriteTimeUtc(target, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.Delete(Clip);
            if (directoryLink)
            {
                Directory.Delete(Path.GetDirectoryName(Clip)!);
                Directory.CreateSymbolicLink(Path.GetDirectoryName(Clip)!, downloads.FullName);
            }
            else
            {
                File.CreateSymbolicLink(Clip, target);
            }

            var original = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
            using (var image = original.Open())
            using (var reader = new UdfReader(image))
            using (var clip = reader.OpenFile("BDMV\\STREAM\\00001.m2ts", FileMode.Open))
            {
                Assert.Equal(bytes.Length, clip.Length);
                var actual = new byte[bytes.Length];
                clip.ReadExactly(actual);
                Assert.Equal(bytes, actual);
            }

            File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(target).AddSeconds(1));
            var modified = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
            Assert.NotEqual(original.Tag, modified.Tag);
            File.WriteAllBytes(target, new byte[16384]);
            var resized = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
            Assert.NotEqual(modified.Tag, resized.Tag);
            Assert.NotEqual(modified.Length, resized.Length);
        }
        finally
        {
            if (directoryLink)
            {
                Directory.Delete(Path.GetDirectoryName(Clip)!);
            }
            else
            {
                File.Delete(Clip);
            }

            downloads.Delete(true);
        }
    }

    [Fact]
    public void Image_RefreshesWhenLinkIsRetargetedWithSameMetadata()
    {
        var first = Path.Combine(_root, "first");
        var second = Path.Combine(_root, "second");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        File.SetLastWriteTimeUtc(second, File.GetLastWriteTimeUtc(first));
        File.Delete(Clip);
        File.CreateSymbolicLink(Clip, first);
        var original = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        File.Delete(Clip);
        File.CreateSymbolicLink(Clip, second);
        var updated = Assert.IsType<VirtualBlurayImage>(_images.GetImage(_root));
        Assert.NotEqual(original.Tag, updated.Tag);
        using var image = updated.Open();
        using var reader = new UdfReader(image);
        using var clip = reader.OpenFile("BDMV\\STREAM\\00001.m2ts", FileMode.Open);
        Assert.Equal(2, clip.ReadByte());
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void Image_RejectsDirectoryLinkCycles(string target)
    {
        Directory.CreateSymbolicLink(Path.Combine(_root, "BDMV", "loop"), target);
        Assert.Null(_images.GetImage(_root));
    }

    [Fact]
    public void Image_AllowsRepeatedDirectoryTargetsOutsideAncestorChain()
    {
        Directory.CreateSymbolicLink(Path.Combine(_root, "BDMV", "STREAM_COPY"), "STREAM");
        using var image = _images.GetImage(_root)!.Open();
        using var reader = new UdfReader(image);
        Assert.True(reader.FileExists("BDMV\\STREAM\\00001.m2ts"));
        Assert.True(reader.FileExists("BDMV\\STREAM_COPY\\00001.m2ts"));
    }

    [Fact]
    public void Image_RejectsBrokenFileLinks()
    {
        File.Delete(Clip);
        File.CreateSymbolicLink(Clip, Path.Combine(_root, "missing"));
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
        _images.Dispose();
        _cache.Dispose();
        Directory.Delete(_root, true);
    }
}
