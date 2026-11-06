using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Images;

public sealed class MusicAlbumImageProviderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<IProviderManager> _providerManager = new();

    public MusicAlbumImageProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task FetchAsync_MusicAlbum_FollowsLibrarySettingForSavingWithMedia()
    {
        var album = new MusicAlbum { Id = Guid.NewGuid() };
        var provider = new MusicAlbumImageProvider(
            Mock.Of<IFileSystem>(),
            _providerManager.Object,
            CreateApplicationPaths(),
            Mock.Of<IImageProcessor>(),
            CreateLibraryManager(new Audio { Id = Guid.NewGuid() }));

        await provider.FetchAsync(album, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifySaved(album, null);
    }

    [Fact]
    public async Task FetchAsync_PhotoAlbum_KeepsImageInMetadataFolder()
    {
        var album = new PhotoAlbum { Id = Guid.NewGuid() };
        var provider = new PhotoAlbumImageProvider(
            Mock.Of<IFileSystem>(),
            _providerManager.Object,
            CreateApplicationPaths(),
            Mock.Of<IImageProcessor>(),
            CreateLibraryManager(new Photo { Id = Guid.NewGuid() }));

        await provider.FetchAsync(album, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifySaved(album, false);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDirectory, true);
    }

    private void VerifySaved(BaseItem album, bool? saveLocallyWithMedia)
    {
        _providerManager.Verify(
            p => p.SaveImage(album, It.IsAny<string>(), It.IsAny<string>(), ImageType.Primary, null, saveLocallyWithMedia, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private IApplicationPaths CreateApplicationPaths()
    {
        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.SetupGet(p => p.TempDirectory).Returns(_tempDirectory);
        return applicationPaths.Object;
    }

    private ILibraryManager CreateLibraryManager(BaseItem child)
    {
        var imagePath = Path.Combine(_tempDirectory, "cover.jpg");
        File.WriteAllBytes(imagePath, [0xFF, 0xD8, 0xFF]);
        child.SetImage(new ItemImageInfo { Path = imagePath, Type = ImageType.Primary }, 0);

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([child]);
        return libraryManager.Object;
    }
}
