using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Playlists;

public class PlaylistManagerShareTokenTests
{
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILibraryMonitor> _mockLibraryMonitor;
    private readonly Mock<ILogger<PlaylistManager>> _mockLogger;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<IProviderManager> _mockProviderManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IDbContextFactory<JellyfinDbContext>> _mockDbProvider;
    private readonly PlaylistManager _playlistManager;

    public PlaylistManagerShareTokenTests()
    {
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockLibraryMonitor = new Mock<ILibraryMonitor>();
        _mockLogger = new Mock<ILogger<PlaylistManager>>();
        _mockUserManager = new Mock<IUserManager>();
        _mockProviderManager = new Mock<IProviderManager>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockDbProvider = new Mock<IDbContextFactory<JellyfinDbContext>>();

        var mockConfigManager = new Mock<IServerConfigurationManager>();
        var mockAppPaths = new Mock<IServerApplicationPaths>();
        mockAppPaths.Setup(p => p.DataPath).Returns("/test/data");
        mockConfigManager.Setup(c => c.ApplicationPaths).Returns(mockAppPaths.Object);

        BaseItem.LibraryManager = _mockLibraryManager.Object;
        BaseItem.FileSystem = _mockFileSystem.Object;
        BaseItem.ConfigurationManager = mockConfigManager.Object;
        BaseItem.Logger = new Mock<ILogger<BaseItem>>().Object;

        _mockLibraryManager.Setup(m => m.GetCollectionFolders(It.IsAny<BaseItem>())).Returns(new List<Folder>());

        var mockDbContext = new Mock<JellyfinDbContext>(new DbContextOptions<JellyfinDbContext>());
        _mockDbProvider.Setup(p => p.CreateDbContext()).Returns(mockDbContext.Object);

        _playlistManager = new PlaylistManager(
            _mockLibraryManager.Object,
            _mockFileSystem.Object,
            _mockLibraryMonitor.Object,
            _mockLogger.Object,
            _mockUserManager.Object,
            _mockProviderManager.Object,
            _mockConfiguration.Object,
            _mockDbProvider.Object);
    }

    [Fact]
    public async Task GenerateShareToken_PlaylistNotFound_ThrowsArgumentException()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockLibraryManager
            .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(Array.Empty<BaseItem>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _playlistManager.GenerateShareToken(playlistId, userId));
    }

    [Fact]
    public async Task GenerateShareToken_UserNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreateMockPlaylist(playlistId, ownerId);

        SetupGetPlaylistsForUser(playlist);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _playlistManager.GenerateShareToken(playlistId, userId));
    }

    [Fact]
    public async Task GenerateShareToken_ValidRequest_ReturnsToken()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreateMockPlaylist(playlistId, userId);

        SetupGetPlaylistsForUser(playlist);
        SetupUpdatePlaylist(playlist);

        // Act
        var token = await _playlistManager.GenerateShareToken(playlistId, userId);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal(64, token.Length); // 32 bytes = 64 hex characters
        Assert.Equal(token, playlist.ShareToken);
    }

    [Fact]
    public async Task GenerateShareToken_CalledTwice_GeneratesNewToken()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreateMockPlaylist(playlistId, userId);

        SetupGetPlaylistsForUser(playlist);
        SetupUpdatePlaylist(playlist);

        // Act
        var token1 = await _playlistManager.GenerateShareToken(playlistId, userId);
        var token2 = await _playlistManager.GenerateShareToken(playlistId, userId);

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.Equal(token2, playlist.ShareToken);
    }

    [Fact]
    public async Task RevokeShareToken_ValidRequest_RemovesToken()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreateMockPlaylist(playlistId, userId);
        playlist.ShareToken = "existing-token";

        SetupGetPlaylistsForUser(playlist);
        SetupUpdatePlaylist(playlist);

        // Act
        await _playlistManager.RevokeShareToken(playlistId, userId);

        // Assert
        Assert.Null(playlist.ShareToken);
    }

    [Fact]
    public async Task RevokeShareToken_UserNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreateMockPlaylist(playlistId, ownerId);

        SetupGetPlaylistsForUser(playlist);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _playlistManager.RevokeShareToken(playlistId, userId));
    }

    [Fact]
    public void GetPlaylistByShareToken_ValidToken_ReturnsPlaylist()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var shareToken = "test-token-12345";
        var playlist = CreateMockPlaylist(playlistId, Guid.NewGuid());
        playlist.ShareToken = shareToken;

        SetupGetAllPlaylists(playlist);

        // Act
        var result = _playlistManager.GetPlaylistByShareToken(shareToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(playlistId, result.Id);
        Assert.Equal(shareToken, result.ShareToken);
    }

    [Fact]
    public void GetPlaylistByShareToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var playlist = CreateMockPlaylist(Guid.NewGuid(), Guid.NewGuid());
        playlist.ShareToken = "valid-token";

        SetupGetAllPlaylists(playlist);

        // Act
        var result = _playlistManager.GetPlaylistByShareToken("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPlaylistByShareToken_EmptyToken_ReturnsNull()
    {
        // Act
        var result = _playlistManager.GetPlaylistByShareToken(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPlaylistByShareToken_NullToken_ReturnsNull()
    {
        // Act
        var result = _playlistManager.GetPlaylistByShareToken(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPlaylistByShareToken_TokenCaseInsensitive_ReturnsPlaylist()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var shareToken = "test-token-abc123";
        var playlist = CreateMockPlaylist(playlistId, Guid.NewGuid());
        playlist.ShareToken = shareToken.ToUpperInvariant();

        SetupGetAllPlaylists(playlist);

        // Act
        var result = _playlistManager.GetPlaylistByShareToken(shareToken.ToLowerInvariant());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(playlistId, result.Id);
    }

    private Playlist CreateMockPlaylist(Guid playlistId, Guid ownerId)
    {
        var playlist = new Playlist
        {
            Id = playlistId,
            OwnerUserId = ownerId,
            Name = "Test Playlist",
            Path = "/test/path"
        };
        return playlist;
    }

    private void SetupGetPlaylistsForUser(Playlist playlist)
    {
        _mockLibraryManager
            .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new[] { playlist });

        var user = new User(
            "testuser",
            typeof(DefaultAuthenticationProvider).FullName!,
            typeof(DefaultPasswordResetProvider).FullName!);
        user.AddDefaultPermissions();
        user.AddDefaultPreferences();

        _mockUserManager
            .Setup(m => m.GetUserById(It.IsAny<Guid>()))
            .Returns(user);
    }

    private void SetupGetAllPlaylists(params Playlist[] playlists)
    {
        var playlistEntities = playlists.Select(p =>
        {
            var entity = new BaseItemEntity
            {
                Id = p.Id,
                Type = typeof(Playlist).ToString(),
                Data = JsonSerializer.Serialize(p, JsonDefaults.Options)
            };
            return entity;
        }).AsQueryable();

        var mockDbSet = new Mock<DbSet<BaseItemEntity>>();
        mockDbSet.As<IQueryable<BaseItemEntity>>().Setup(m => m.Provider).Returns(playlistEntities.Provider);
        mockDbSet.As<IQueryable<BaseItemEntity>>().Setup(m => m.Expression).Returns(playlistEntities.Expression);
        mockDbSet.As<IQueryable<BaseItemEntity>>().Setup(m => m.ElementType).Returns(playlistEntities.ElementType);
        mockDbSet.As<IQueryable<BaseItemEntity>>().Setup(m => m.GetEnumerator()).Returns(playlistEntities.GetEnumerator());

        var mockDbContext = new Mock<JellyfinDbContext>(new DbContextOptions<JellyfinDbContext>());
        mockDbContext.Setup(c => c.BaseItems).Returns(mockDbSet.Object);
        _mockDbProvider.Setup(p => p.CreateDbContext()).Returns(mockDbContext.Object);

        foreach (var playlist in playlists)
        {
            _mockLibraryManager
                .Setup(m => m.GetItemById(playlist.Id))
                .Returns(playlist);
        }
    }

    private void SetupUpdatePlaylist(Playlist playlist)
    {
        _mockLibraryManager
            .Setup(m => m.GetItemById(It.IsAny<Guid>()))
            .Returns(playlist);
    }
}
