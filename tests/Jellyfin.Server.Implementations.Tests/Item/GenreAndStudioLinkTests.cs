using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers items linking to the genre and studio items they belong to rather than carrying the name.
/// </summary>
public sealed class GenreAndStudioLinkTests : IDisposable
{
    private static readonly Guid _movieId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid _otherMovieId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid _genreId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid _musicGenreId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid _studioId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public GenreAndStudioLinkTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();

            AddItem(ctx, _movieId, BaseItemKind.Movie, "Blade Runner");
            AddItem(ctx, _otherMovieId, BaseItemKind.Movie, "Alien");

            AddItem(ctx, _genreId, BaseItemKind.Genre, "Sci-Fi");
            AddItem(ctx, _musicGenreId, BaseItemKind.MusicGenre, "Sci-Fi");
            AddItem(ctx, _studioId, BaseItemKind.Studio, "Warner Bros.");

            ctx.BaseItemGenres.Add(new BaseItemGenre { Item = null!, ItemId = _movieId, GenreItemId = _genreId });
            ctx.BaseItemStudios.Add(new BaseItemStudio { Item = null!, ItemId = _movieId, StudioItemId = _studioId });
            ctx.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            _itemTypeLookup,
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void GetItemList_GenreIds_ReturnsWhatLinksToTheGenre()
    {
        var result = _repository.GetItemList(new InternalItemsQuery { GenreIds = [_genreId] });

        var item = Assert.Single(result);
        Assert.Equal(_movieId, item.Id);
    }

    [Fact]
    public void GetItemList_GenreIds_DoesNotMatchAMusicGenreOfTheSameName()
    {
        // Matching on the clean name alone made a genre and a music genre of one name interchangeable.
        var result = _repository.GetItemList(new InternalItemsQuery { GenreIds = [_musicGenreId] });

        Assert.Empty(result);
    }

    [Fact]
    public void GetItemList_GenreIds_AfterTheGenreWasRenamed_StillReturnsIt()
    {
        Rename(_genreId, "Science Fiction");

        var result = _repository.GetItemList(new InternalItemsQuery { GenreIds = [_genreId] });

        Assert.Single(result);
    }

    [Fact]
    public void GetItemList_Genres_MatchesTheNameTheGenreItemCarriesNow()
    {
        Rename(_genreId, "Science Fiction");

        Assert.Single(_repository.GetItemList(new InternalItemsQuery { Genres = ["Science Fiction"] }));
        Assert.Empty(_repository.GetItemList(new InternalItemsQuery { Genres = ["Sci-Fi"] }));
    }

    [Fact]
    public void GetItemList_Genres_SpellsThatCleanAlikeAreOneGenre()
    {
        Assert.Single(_repository.GetItemList(new InternalItemsQuery { Genres = ["Sci Fi"] }));
    }

    [Fact]
    public void GetItemList_StudioIds_ReturnsWhatLinksToTheStudio()
    {
        var result = _repository.GetItemList(new InternalItemsQuery { StudioIds = [_studioId] });

        var item = Assert.Single(result);
        Assert.Equal(_movieId, item.Id);
    }

    [Fact]
    public void GetItemList_IsDeadGenre_KeepsARenamedGenreAndDropsAnUnusedOne()
    {
        Rename(_genreId, "Science Fiction");

        var result = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Genre, BaseItemKind.MusicGenre],
            IsDeadGenre = true
        });

        var item = Assert.Single(result);
        Assert.Equal(_musicGenreId, item.Id);
    }

    [Fact]
    public void GetItemByNameLinks_ReportsTheLinkedItemsPerItem()
    {
        var links = _repository.GetItemByNameLinks([_movieId, _otherMovieId]);

        var movie = links[_movieId];
        Assert.Equal(_genreId, Assert.Single(movie.Genres).Id);
        Assert.Equal(_studioId, Assert.Single(movie.Studios).Id);

        Assert.False(links.ContainsKey(_otherMovieId));
    }

    [Fact]
    public void GetItemByNameLinks_ReportsTheNameTheLinkedItemCarriesNow()
    {
        Rename(_genreId, "Science Fiction");

        var links = _repository.GetItemByNameLinks([_movieId]);

        Assert.Equal("Science Fiction", Assert.Single(links[_movieId].Genres).Name);
    }

    [Fact]
    public void MusicGenreTypes_CoversEverythingThatHasMusicGenres()
    {
        // The migration picks the genre kind from this list while a save picks it from the interface.
        Assert.Contains(typeof(AudioBook).FullName, _itemTypeLookup.MusicGenreTypes);
        Assert.Contains(typeof(MediaBrowser.Controller.Entities.Audio.Audio).FullName, _itemTypeLookup.MusicGenreTypes);
        Assert.DoesNotContain(typeof(MediaBrowser.Controller.Entities.Movies.Movie).FullName, _itemTypeLookup.MusicGenreTypes);
    }

    private void Rename(Guid id, string name)
    {
        using var ctx = CreateDbContext();
        var item = ctx.BaseItems.Single(e => e.Id.Equals(id));
        item.Name = name;
        item.CleanName = name.GetCleanValue();
        ctx.SaveChanges();
    }

    private void AddItem(JellyfinDbContext ctx, Guid id, BaseItemKind kind, string name)
    {
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            Name = name,
            CleanName = name.GetCleanValue(),
            IsFolder = false,
            IsVirtualItem = false
        });
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
