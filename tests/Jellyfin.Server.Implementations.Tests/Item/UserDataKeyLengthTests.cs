using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The key of the user data of an item can be longer than its column: the key of a song holds its title, album and
/// artists. The key is shortened wherever it is stored or looked up, so that the row is saved, found again and
/// reattached.
/// </summary>
public sealed class UserDataKeyLengthTests : JellyfinDbTestFixture
{
    // Longer than the bound with and without the NUL character, so that the two give different shortened keys.
    private static readonly string _nulKeyPrefix = new('t', KeyNormalizer.UserDataKeyMaxLength);
    private static readonly string _nulKey = _nulKeyPrefix + "\0uu";
    private static readonly string _cleanedKey = KeyNormalizer.NormalizeUserDataKey(_nulKeyPrefix + "uu");

    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public UserDataKeyLengthTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            DatabaseProvider,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Fact]
    public void SaveUserData_SongKeyOverTheBound_StoresTheShortenedKey()
    {
        var song = CreateSong();
        var key = song.GetUserDataKeys()[0];
        Assert.True(key.Length > KeyNormalizer.UserDataKeyMaxLength);
        SeedItem(song);
        var user = CreateUser();

        CreateUserDataManager().SaveUserData(user, song, new UserItemData { Key = key, Played = true }, UserDataSaveReason.TogglePlayed, CancellationToken.None);

        using var ctx = CreateDbContext();
        var keys = ctx.UserData.Where(e => e.ItemId.Equals(song.Id)).Select(e => e.CustomDataKey).ToList();
        Assert.Contains(KeyNormalizer.NormalizeUserDataKey(key), keys);
        Assert.All(keys, k => Assert.True(k.Length <= KeyNormalizer.UserDataKeyMaxLength));
    }

    [Fact]
    public void SaveUserData_SongKeyOverTheBoundSavedTwice_UpdatesTheStoredRow()
    {
        var song = CreateSong();
        SeedItem(song);
        var manager = CreateUserDataManager();
        var user = CreateUser();

        manager.SaveUserData(user, song, new UserItemData { Key = "ignored", PlayCount = 1 }, UserDataSaveReason.PlaybackFinished, CancellationToken.None);
        manager.SaveUserData(user, song, new UserItemData { Key = "ignored", PlayCount = 2 }, UserDataSaveReason.PlaybackFinished, CancellationToken.None);

        using var ctx = CreateDbContext();
        var rows = ctx.UserData.Where(e => e.ItemId.Equals(song.Id)).ToList();
        Assert.Equal(song.GetUserDataKeys().Count, rows.Count);
        Assert.All(rows, row => Assert.Equal(2, row.PlayCount));
    }

    [Fact]
    public void GetUserData_SongKeyOverTheBound_ReturnsTheSavedRow()
    {
        var song = CreateSong();
        SeedItem(song);
        var user = CreateUser();
        CreateUserDataManager().SaveUserData(user, song, new UserItemData { Key = "ignored", PlayCount = 4 }, UserDataSaveReason.PlaybackFinished, CancellationToken.None);
        using (var ctx = CreateDbContext())
        {
            song.UserData = ctx.UserData.Where(e => e.ItemId.Equals(song.Id)).ToList();
        }

        var userData = CreateUserDataManager().GetUserData(user, song);

        Assert.Equal(4, userData?.PlayCount);
        Assert.Equal(KeyNormalizer.NormalizeUserDataKey(song.GetUserDataKeys()[0]), userData?.Key);
    }

    [Fact]
    public void GetUserDataBatch_SongKeyOverTheBound_ReturnsTheSavedRow()
    {
        var song = CreateSong();
        SeedItem(song);
        var user = CreateUser();
        CreateUserDataManager().SaveUserData(user, song, new UserItemData { Key = "ignored", PlayCount = 4 }, UserDataSaveReason.PlaybackFinished, CancellationToken.None);

        var result = CreateUserDataManager().GetUserDataBatch([song], user);

        Assert.Equal(4, result[song.Id].PlayCount);
    }

    [Fact]
    public void GetUserDataBatch_SongWithoutRowsAndKeyOverTheBound_ReturnsAKeyThatFitsTheColumn()
    {
        var song = CreateSong();
        SeedItem(song);

        var result = CreateUserDataManager().GetUserDataBatch([song], CreateUser());

        Assert.True(result[song.Id].Key.Length <= KeyNormalizer.UserDataKeyMaxLength);
    }

    [Fact]
    public async Task ReattachUserData_KeyOverTheBound_ReattachesTheDetachedRowStoredUnderTheShortenedKey()
    {
        var movie = CreateMovie(new string('t', KeyNormalizer.UserDataKeyMaxLength + 40));
        var shortened = KeyNormalizer.NormalizeUserDataKey(movie.GetUserDataKeys()[0]);
        SeedItem(movie);
        using (var ctx = CreateDbContext())
        {
            ctx.UserData.Add(new UserData
            {
                ItemId = BaseItemRepository.PlaceholderId,
                Item = null,
                UserId = _userId,
                User = null,
                CustomDataKey = shortened,
                PlayCount = 3,
                RetentionDate = new DateTime(2025, 6, 22, 0, 0, 0, DateTimeKind.Utc)
            });
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            var rows = ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).ToList();
            Assert.Contains(shortened, rows.Select(e => e.CustomDataKey));
            Assert.All(rows, row => Assert.Equal(3, row.PlayCount));
            Assert.All(rows, row => Assert.True(row.CustomDataKey.Length <= KeyNormalizer.UserDataKeyMaxLength));
            Assert.Empty(ctx.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)));
        }
    }

    [Fact]
    public void GetUserData_KeyWithNulOverTheBoundAndNoRows_ReturnsTheKeyWithoutTheNulShortened()
    {
        var movie = CreateMovie(_nulKey);
        SeedItem(movie);

        var userData = CreateUserDataManager().GetUserData(CreateUser(), movie);

        Assert.Equal(_cleanedKey, userData?.Key);
    }

    [Fact]
    public void GetUserDataBatch_KeyWithNulOverTheBoundAndNoRows_ReturnsTheKeyWithoutTheNulShortened()
    {
        var movie = CreateMovie(_nulKey);
        SeedItem(movie);

        var result = CreateUserDataManager().GetUserDataBatch([movie], CreateUser());

        Assert.Equal(_cleanedKey, result[movie.Id].Key);
    }

    [Fact]
    public Task ReattachUserData_KeyWithNulOverTheBound_ReattachesTheRowStoredUnderTheCleanedShortenedKey()
        => ReattachRowStoredUnderAsync(_cleanedKey);

    [Fact]
    public Task ReattachUserData_KeyWithNulOverTheBound_ReattachesTheRowStoredUnderTheShortenedKeyWithTheNul()
        => ReattachRowStoredUnderAsync(KeyNormalizer.NormalizeUserDataKey(_nulKey));

    private async Task ReattachRowStoredUnderAsync(string storedKey)
    {
        var movie = CreateMovie(_nulKey);
        SeedItem(movie);
        using (var ctx = CreateDbContext())
        {
            ctx.UserData.Add(new UserData
            {
                ItemId = BaseItemRepository.PlaceholderId,
                Item = null,
                UserId = _userId,
                User = null,
                CustomDataKey = storedKey,
                PlayCount = 3,
                RetentionDate = new DateTime(2025, 6, 22, 0, 0, 0, DateTimeKind.Utc)
            });
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            var rows = ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).ToList();
            Assert.Contains(_cleanedKey, rows.Select(e => e.CustomDataKey));
            Assert.All(rows, row => Assert.Equal(3, row.PlayCount));
            Assert.Empty(ctx.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)));
        }
    }

    private static Audio CreateSong()
    {
        return new Audio
        {
            Id = Guid.NewGuid(),
            Name = "Götterdämmerung, WWV 86D, Act III Scene 3: \"Starke Scheite schichtet mir dort am Rande des Rheins zu Hauf!\" (Brünnhilde, Gutrune, Hagen)",
            Album = "Wagner: Der Ring des Nibelungen - Götterdämmerung, WWV 86D (Live at Bayreuther Festspiele 2008, Remastered)",
            AlbumArtists = ["Bayreuther Festspielorchester"],
            IndexNumber = 21,
            ParentIndexNumber = 4
        };
    }

    private static Movie CreateMovie(string imdbId)
    {
        return new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Black Widow",
            ProviderIds = new() { ["Imdb"] = imdbId }
        };
    }

    private User CreateUser() => new("user", "auth-provider", "reset-provider") { Id = _userId };

    private UserDataManager CreateUserDataManager()
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        return new UserDataManager(configurationManager.Object, CreateDbContextFactory());
    }

    private void SeedItem(BaseItem item)
    {
        using var ctx = CreateDbContext();
        if (!ctx.Users.Any(e => e.Id.Equals(_userId)))
        {
            ctx.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
        }

        if (!ctx.BaseItems.Any(e => e.Id.Equals(BaseItemRepository.PlaceholderId)))
        {
            ctx.BaseItems.Add(new BaseItemEntity { Id = BaseItemRepository.PlaceholderId, Type = typeof(Folder).FullName! });
        }

        ctx.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = item.GetType().FullName! });
        ctx.SaveChanges();
    }
}
