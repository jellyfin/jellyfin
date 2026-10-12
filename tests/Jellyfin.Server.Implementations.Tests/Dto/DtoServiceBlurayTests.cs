using System;
using System.IO;
using Emby.Server.Implementations.Dto;
using Emby.Server.Implementations.Library;
using MediaBrowser.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Dto;

public class DtoServiceBlurayTests
{
    [Theory]
    [InlineData(".DS_Store")]
    [InlineData("._index.bdmv")]
    [InlineData("Thumbs.db")]
    [InlineData("@eaDir/thumbnail")]
    public void VirtualImage_UsesLibraryIgnoreRules(string ignoredPath)
    {
        var root = Directory.CreateTempSubdirectory(".bluray-test-");
        try
        {
            var bdmv = Directory.CreateDirectory(Path.Combine(root.FullName, "BDMV"));
            File.WriteAllBytes(Path.Combine(bdmv.FullName, "index.bdmv"), [1]);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var images = new VirtualBlurayImageManager(
                cache,
                NullLogger<VirtualBlurayImageManager>.Instance,
                path => IgnorePatterns.ShouldIgnore(path));
            var original = Assert.IsType<VirtualBlurayImage>(images.GetImage(root.FullName));
            var ignored = Path.Combine(bdmv.FullName, ignoredPath);
            Directory.CreateDirectory(Path.GetDirectoryName(ignored)!);
            File.WriteAllBytes(ignored, [2]);
            Assert.Same(original, images.GetImage(root.FullName));
            File.WriteAllBytes(ignored, [2, 3]);
            Assert.Same(original, images.GetImage(root.FullName));
            File.WriteAllBytes(Path.Combine(bdmv.FullName, "index.bdmv"), [1, 2]);
            Assert.NotEqual(original.Tag, images.GetImage(root.FullName)!.Tag);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Theory]
    [InlineData(1, false, "bluray")]
    [InlineData(1, true, "bluray")]
    [InlineData(4, false, "bluray")]
    [InlineData(4, true, "bluray")]
    [InlineData(1, false, "ts")]
    [InlineData(1, true, "ts")]
    [InlineData(4, false, "ts")]
    [InlineData(4, true, "ts")]
    public void GetBaseItemDto_Bluray_PreservesNativePathWithIsoSource(
        int clips,
        bool includeSources,
        string container)
    {
        var root = Directory.CreateTempSubdirectory();
        var originalMediaSourceManager = BaseItem.MediaSourceManager;
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "BDMV"));
            File.WriteAllBytes(Path.Combine(root.FullName, "BDMV", "index.bdmv"), [1]);
            var streams = Directory.CreateDirectory(Path.Combine(root.FullName, "BDMV", "STREAM"));
            for (var i = 1; i <= clips; i++)
            {
                File.WriteAllBytes(Path.Combine(streams.FullName, $"{i:D5}.m2ts"), [1]);
            }

            var movieMock = new Mock<Movie> { CallBase = true };
            movieMock.SetupGet(movie => movie.LocationType).Returns(LocationType.FileSystem);
            movieMock.SetupGet(movie => movie.SourceType).Returns(SourceType.Library);
            movieMock.Setup(movie => movie.GetClientTypeName()).Returns(nameof(Movie));
            var movie = movieMock.Object;
            movie.Id = Guid.NewGuid();
            movie.Name = "Disc";
            movie.Path = root.FullName;
            movie.VideoType = VideoType.BluRay;
            movie.Container = container;
            var sources = new Mock<IMediaSourceManager>();
            sources
                .Setup(manager => manager.GetPathProtocol(movie.Path))
                .Returns(MediaProtocol.File);
            BaseItem.MediaSourceManager = sources.Object;
            var library = new Mock<ILibraryManager>();
            library
                .Setup(manager => manager.GetPathAfterNetworkSubstitution(movie.Path, movie))
                .Returns(movie.Path);
            sources
                .Setup(manager => manager.GetStaticMediaSources(movie, true, null))
                .Returns(
                    [
                        new MediaSourceInfo
                        {
                            Id = movie.Id.ToString("N"),
                            Path = movie.Path,
                            Protocol = MediaProtocol.File,
                            VideoType = VideoType.BluRay,
                            Container = "ts",
                        },
                    ]);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var recordings = Mock.Of<IRecordingsManager>();
            var service = new DtoService(
                NullLogger<DtoService>.Instance,
                library.Object,
                Mock.Of<IUserDataManager>(),
                Mock.Of<IImageProcessor>(),
                Mock.Of<IProviderManager>(),
                recordings,
                Mock.Of<IApplicationHost>(),
                sources.Object,
                new Lazy<ILiveTvManager>(() => Mock.Of<ILiveTvManager>()),
                Mock.Of<ITrickplayManager>(),
                Mock.Of<IChapterManager>(),
                new VirtualBlurayImageManager(
                    cache,
                    NullLogger<VirtualBlurayImageManager>.Instance,
                    _ => false));
            var options = new DtoOptions(false)
            {
                EnableImages = false,
                Fields = includeSources
                    ? [ItemFields.Path, ItemFields.MediaSources]
                    : [ItemFields.Path],
            };

            var dto = service.GetBaseItemDto(movie, options);

            Assert.Equal(VideoType.BluRay, dto.VideoType);
            Assert.Equal(container, dto.Container);
            Assert.Equal(movie.IsoType, dto.IsoType);
            var expectedPath = $"{root.FullName}.iso";
            Assert.Equal(root.FullName, dto.Path);
            Assert.Equal(VideoType.BluRay, movie.VideoType);
            Assert.Equal(root.FullName, movie.Path);
            if (includeSources)
            {
                var source = Assert.Single(dto.MediaSources);
                Assert.Equal(VideoType.Iso, source.VideoType);
                Assert.Equal("iso", source.Container);
                Assert.Equal(IsoType.BluRay, source.IsoType);
                Assert.Equal(expectedPath, source.Path);
                using var images = new VirtualBlurayImageManager(
                    cache,
                    NullLogger<VirtualBlurayImageManager>.Instance,
                    _ => false);
                var image = Assert.IsType<VirtualBlurayImage>(images.GetImage(root.FullName));
                Assert.Equal(image.Length, source.Size);
                Assert.Equal(image.Tag, source.ETag);
            }
            else
            {
                Assert.False(cache.TryGetValue((typeof(VirtualBlurayImage), root.FullName), out _));
            }
        }
        finally
        {
            root.Delete(true);
            BaseItem.MediaSourceManager = originalMediaSourceManager;
        }
    }
}
