using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

// put tests that mock the static LibraryManager in the same collection to avoid test interference
[Collection("LibraryManagerTests")]
public sealed class MusicAlbumTests : IDisposable
{
    private readonly ILibraryManager? _previousLibraryManager = BaseItem.LibraryManager;
    private readonly IProviderManager? _previousProviderManager = BaseItem.ProviderManager;

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        BaseItem.ProviderManager = _previousProviderManager;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshAllMetadata_CreatesMissingTrackArtists(bool isAutomated)
    {
        var (album, libraryManager, _) = SetupCompilation();

        await RefreshAsync(album, isAutomated);

        // The library scan and the real-time file monitor are both automated, so gating this on a
        // user-initiated refresh left newly imported compilations with unresolvable track artists
        libraryManager.Verify(x => x.GetArtist("The Mark Harvey Group"), Times.Once());
        libraryManager.Verify(x => x.GetArtist("The Phill Musra Group"), Times.Once());
        libraryManager.Verify(x => x.GetArtist("Various Artists"), Times.Once());
    }

    [Fact]
    public async Task RefreshAllMetadata_DoesNotRecreateExistingTrackArtists()
    {
        var (album, libraryManager, _) = SetupCompilation();

        var existing = new MusicArtist { Id = Guid.NewGuid(), ParentId = Guid.NewGuid(), Name = "The Phill Musra Group" };
        libraryManager
            .Setup(x => x.GetArtists(It.IsAny<IReadOnlyList<string>>()))
            .Returns(new Dictionary<string, MusicArtist[]> { ["The Phill Musra Group"] = [existing] });

        await RefreshAsync(album, isAutomated: true);

        libraryManager.Verify(x => x.GetArtist("The Phill Musra Group"), Times.Never());
        libraryManager.Verify(x => x.GetArtist("The Mark Harvey Group"), Times.Once());
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 2)]
    public async Task RefreshAllMetadata_OnlyRefreshesArtistMetadataWhenUserInitiated(bool isAutomated, int expectedRefreshes)
    {
        var (album, _, providerManager) = SetupCompilation();

        await RefreshAsync(album, isAutomated);

        // "Various Artists" resolves to a library folder here, leaving the two by-name track artists
        providerManager.Verify(
            x => x.RefreshSingleItem(It.IsAny<MusicArtist>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()),
            Times.Exactly(expectedRefreshes));
    }

    private static Task RefreshAsync(MusicAlbum album, bool isAutomated)
        => album.RefreshAllMetadata(
            new MetadataRefreshOptions(Mock.Of<IDirectoryService>()) { IsAutomated = isAutomated },
            new Progress<double>(),
            CancellationToken.None);

    private static (MusicAlbum Album, Mock<ILibraryManager> LibraryManager, Mock<IProviderManager> ProviderManager) SetupCompilation()
    {
        var album = new MusicAlbum
        {
            Id = Guid.NewGuid(),
            ParentId = Guid.NewGuid(),
            Name = "The Boston Creative Jazz Scene: 1970-1983",
            AlbumArtists = ["Various Artists"]
        };

        album.Children =
        [
            new Audio { Id = Guid.NewGuid(), ParentId = album.Id, Name = "For Margot", Artists = ["The Mark Harvey Group"] },
            new Audio { Id = Guid.NewGuid(), ParentId = album.Id, Name = "Egypt", Artists = ["The Phill Musra Group"] }
        ];

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.GetArtists(It.IsAny<IReadOnlyList<string>>()))
            .Returns(new Dictionary<string, MusicArtist[]>());
        libraryManager
            .Setup(x => x.UpdateImagesAsync(It.IsAny<BaseItem>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        // An artist with a parent is a library folder, one without is accessed by name
        libraryManager
            .Setup(x => x.GetArtist(It.IsAny<string>()))
            .Returns((string name) => new MusicArtist { Id = Guid.NewGuid(), Name = name });
        libraryManager
            .Setup(x => x.GetArtist("Various Artists"))
            .Returns(new MusicArtist { Id = Guid.NewGuid(), ParentId = Guid.NewGuid(), Name = "Various Artists" });

        var providerManager = new Mock<IProviderManager>();
        providerManager
            .Setup(x => x.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ItemUpdateType.None);

        // AlbumMetadataService copies the track artists onto the album, so the names only become
        // visible to RefreshArtists once the album's own refresh has run
        providerManager
            .Setup(x => x.RefreshSingleItem(album, It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                album.Artists = [.. album.Children.OfType<Audio>().SelectMany(i => i.Artists).Distinct(StringComparer.OrdinalIgnoreCase)];
                return Task.FromResult(ItemUpdateType.None);
            });

        BaseItem.LibraryManager = libraryManager.Object;
        BaseItem.ProviderManager = providerManager.Object;

        return (album, libraryManager, providerManager);
    }
}
