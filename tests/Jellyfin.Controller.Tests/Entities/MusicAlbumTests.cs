using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public sealed class MusicAlbumTests : IDisposable
{
    private static readonly MethodInfo _refreshArtistsMethod = GetReflectionMethod("RefreshArtists");

    private readonly ILibraryManager? _previousLibraryManager = BaseItem.LibraryManager;
    private readonly IProviderManager? _previousProviderManager = BaseItem.ProviderManager;

    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IProviderManager> _providerManager = new();

    public MusicAlbumTests()
    {
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.ProviderManager = _providerManager.Object;
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        BaseItem.ProviderManager = _previousProviderManager;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshArtists_MusicArtistMatchesAnyArtist_LeavesMusicArtistUnchanged(bool useAlbumArtist)
    {
        var artist = MakeArtist("Rush", isFolder: true);
        string[] artistNames = ["Rush"];
        var album = MakeAlbum(
            artist,
            albumArtists: useAlbumArtist ? artistNames : null,
            artists: useAlbumArtist ? null : artistNames);

        // Minimal mocks to keep test scoped to the method's decision logic
        MusicArtist? resolvedArtist = null;

        _libraryManager.Setup(x => x.GetItemById(artist.Id)).Returns(() => resolvedArtist = artist);
        _libraryManager.Setup(x => x.GetArtist(artistNames[0])).Returns(artist);

        await InvokeRefreshArtists(album, CreateRefreshOptions());

        Assert.Equal("Rush", artist.Name);

        // For unchanged cases, expect 'GetItemById' to be called once when 'album.MusicArtist' is accessed
        // It should also resolve a non-null 'MusicArtist', or the verifications below would always pass
        VerifyGetItemByIdCalledOnce(artist.Id);

        Assert.Same(artist, resolvedArtist);

        VerifyNoRefreshMetadataCall();
        VerifyNoUpdateToRepositoryCall();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshArtists_MusicArtistDiffersByCase_OnlyRenamesAndUpdatesMusicArtist(bool useAlbumArtist)
    {
        var artist = MakeArtist("RUSH", isFolder: true);
        string[] artistNames = ["Rush"];
        var album = MakeAlbum(
            artist,
            albumArtists: useAlbumArtist ? artistNames : null,
            artists: useAlbumArtist ? null : artistNames);

        // Minimal mocks to keep test scoped to the method's decision logic
        _libraryManager.Setup(x => x.GetItemById(artist.Id)).Returns(artist);
        _libraryManager.Setup(x => x.GetArtist(artistNames[0])).Returns(artist);

        await InvokeRefreshArtists(album, CreateRefreshOptions());

        Assert.Equal("Rush", artist.Name);

        VerifyNoRefreshMetadataCall();

        _libraryManager.Verify(
            x => x.UpdateItemAsync(artist, It.IsAny<BaseItem>(), ItemUpdateType.MetadataImport, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshArtists_MusicArtistDiffersBeyondCase_RenamesAndFullyRefreshesMusicArtist(bool useAlbumArtist)
    {
        var artist = MakeArtist("Rush5000", isFolder: true);
        string[] artistNames = ["Rush"];
        var album = MakeAlbum(
            artist,
            albumArtists: useAlbumArtist ? artistNames : null,
            artists: useAlbumArtist ? null : artistNames);

        // Minimal mocks to keep test scoped to the method's decision logic
        _libraryManager.Setup(x => x.GetItemById(artist.Id)).Returns(artist);
        _libraryManager.Setup(x => x.GetArtist(artistNames[0])).Returns(artist);

        var options = CreateRefreshOptions();

        await InvokeRefreshArtists(album, options);

        Assert.Equal("Rush", artist.Name);

        // Expect options to be a modified copy with force save and fully refresh options
        _providerManager.Verify(
            x => x.RefreshSingleItem(
                artist,
                It.Is<MetadataRefreshOptions>(o =>
                    !ReferenceEquals(options, o)
                    && o.ForceSave
                    && o.ImageRefreshMode == MetadataRefreshMode.FullRefresh
                    && o.MetadataRefreshMode == MetadataRefreshMode.FullRefresh),
                It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyNoUpdateToRepositoryCall();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshArtists_MusicArtistIsNotFolder_RefreshesWithOriginalOptions(bool useAlbumArtist)
    {
        // Expect albums with no 'MusicArtist' parent folder to create a non-folder backed 'MusicArtist'
        // as metadata. For this test case, 'MakeAlbum' doesn't require the 'artist' parameter
        var artist = MakeArtist("Rush", isFolder: false);
        string[] artistNames = ["Rush"];
        var album = MakeAlbum(
            albumArtists: useAlbumArtist ? artistNames : null,
            artists: useAlbumArtist ? null : artistNames);

        // Minimal mocks to keep test scoped to the method's decision logic
        _libraryManager.Setup(x => x.GetArtist(artistNames[0], It.IsAny<DtoOptions>())).Returns(artist);
        _libraryManager.Setup(x => x.GetArtist(artistNames[0])).Returns(artist);

        var options = CreateRefreshOptions();

        await InvokeRefreshArtists(album, options);

        Assert.Equal("Rush", artist.Name);

        // The 'album.MusicArtist' getter implementation refers to 'AlbumArtist' but not 'Artists' as a fallback
        // Expect 'MusicArtist' to be resolved via 'ILibraryManager.GetArtist(name, dto)' only when 'useAlbumArtist = true'
        _libraryManager.Verify(
            x => x.GetArtist(artistNames[0], It.IsAny<DtoOptions>()),
            useAlbumArtist ? Times.Once : Times.Never);

        // Expect the original options to be passed through directly
        _providerManager.Verify(
            x => x.RefreshSingleItem(artist, options, It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyNoUpdateToRepositoryCall();
    }

    [Fact]
    public async Task RefreshArtists_AlbumMissingArtistInfo_LeavesMusicArtistUnchanged()
    {
        var artist = MakeArtist("Rush", isFolder: true);
        var album = MakeAlbum(artist);

        // Minimal mocks to keep test scoped to the method's decision logic
        MusicArtist? resolvedArtist = null;

        _libraryManager.Setup(x => x.GetItemById(artist.Id)).Returns(() => resolvedArtist = artist);

        await InvokeRefreshArtists(album, CreateRefreshOptions());

        Assert.Equal("Rush", artist.Name);

        // For unchanged cases, expect 'GetItemById' to be called once when 'album.MusicArtist' is accessed
        // It should also resolve a non-null 'MusicArtist', or the verifications below would always pass
        VerifyGetItemByIdCalledOnce(artist.Id);

        Assert.Same(artist, resolvedArtist);

        VerifyNoRefreshMetadataCall();
        VerifyNoUpdateToRepositoryCall();

        // Expect 'ILibraryManager.GetArtist(name)' to never be called (method never enters loop)
        _libraryManager.Verify(x => x.GetArtist(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshArtists_MusicArtistMatchesSecondaryArtist_LeavesMusicArtistUnchanged()
    {
        // Hypothetical scenario when 'MusicArtist' matches a secondary artist
        var albumArtist = MakeArtist("AlbumArtist", isFolder: false);
        var artist = MakeArtist("Artist", isFolder: true);
        string[] albumArtists = ["AlbumArtist"];
        string[] artists = ["Artist"];
        var album = MakeAlbum(artist, albumArtists: albumArtists, artists: artists);

        // Minimal mocks to keep test scoped to the method's decision logic
        MusicArtist? resolvedArtist = null;

        _libraryManager.Setup(x => x.GetItemById(artist.Id)).Returns(() => resolvedArtist = artist);
        _libraryManager.Setup(x => x.GetArtist(albumArtists[0])).Returns(albumArtist);
        _libraryManager.Setup(x => x.GetArtist(artists[0])).Returns(artist);

        var options = CreateRefreshOptions();

        await InvokeRefreshArtists(album, options);

        Assert.Equal("Artist", artist.Name);

        // For unchanged cases, expect 'GetItemById' to be called once when 'album.MusicArtist' is accessed
        // It should also resolve a non-null 'MusicArtist', or the verifications below would always pass
        VerifyGetItemByIdCalledOnce(artist.Id);

        Assert.Same(artist, resolvedArtist);

        // Expect 'artist' not to be refreshed
        _providerManager.Verify(
            x => x.RefreshSingleItem(artist, It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyNoUpdateToRepositoryCall();

        // Expect the method loop to refresh 'albumArtist' with original options
        _providerManager.Verify(
            x => x.RefreshSingleItem(albumArtist, options, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static MethodInfo GetReflectionMethod(string methodName)
    {
        var method = typeof(MusicAlbum).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        return method;
    }

    /// <summary>
    /// Creates a minimal dummy <see cref="MusicArtist"/>. When <paramref name="isFolder"/> is true,
    /// 'ParentId' is set to simulate a folder-backed 'MusicArtist', which the loop in
    /// 'MusicAlbum.RefreshArtists' will skip.
    /// </summary>
    private static MusicArtist MakeArtist(string name = "Artist", bool isFolder = true)
    {
        // A null Path keeps 'MusicArtist.SupportsOwnedItems' false. 'MusicArtist.RefreshMetadata(...)'
        // calls will then fall straight through to 'ProviderManager.RefreshSingleItem(...)'. Tests can then
        // assert on the fall through as a proxy to the non-virtual 'MusicArtist.RefreshMetadata(...)'.
        var artist = new MusicArtist { Id = Guid.NewGuid(), Name = name, Path = null };

        if (isFolder)
        {
            artist.ParentId = Guid.NewGuid();
        }

        return artist;
    }

    private static MusicAlbum MakeAlbum(MusicArtist? artist = null, string name = "Album", string[]? albumArtists = null, string[]? artists = null)
    {
        var album = new MusicAlbum
        {
            Id = Guid.NewGuid(),
            Name = name,
            AlbumArtists = albumArtists ?? [],
            Artists = artists ?? []
        };

        if (artist is not null)
        {
            album.ParentId = artist.Id;
        }

        return album;
    }

    private static MetadataRefreshOptions CreateRefreshOptions()
        => new(Mock.Of<IDirectoryService>());

    private static Task InvokeRefreshArtists(MusicAlbum album, MetadataRefreshOptions options)
        => (Task)_refreshArtistsMethod.Invoke(album, [options, CancellationToken.None])!;

    private void VerifyGetItemByIdCalledOnce(Guid artistId)
    {
        _libraryManager.Verify(x => x.GetItemById(artistId), Times.Once);
    }

    private void VerifyNoRefreshMetadataCall()
    {
        // 'RefreshSingleItem' is checked as a proxy to non-virtual method 'BaseItem.RefreshMetadata'
        _providerManager.Verify(
            x => x.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyNoUpdateToRepositoryCall()
    {
        // 'ILibraryManager.UpdateItemAsync' is checked as a proxy to 'BaseItem.UpdateToRepositoryAsync'
        // Mocking 'MusicArtist' isn't a preferred alternative, as it requires more setup and is harder to maintain
        _libraryManager.Verify(
            x => x.UpdateItemAsync(It.IsAny<BaseItem>(), It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
