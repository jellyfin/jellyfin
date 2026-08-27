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

            AddItem(ctx, _movieId, BaseItemKind.Movie, "Blade Runner", genres: "Sci-Fi|Noir", studios: "Warner Bros.");
            AddItem(ctx, _otherMovieId, BaseItemKind.Movie, "Alien", genres: "Sci-Fi", studios: "Warner Bros.");

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

    // The writer runs this before it saves the renamed item, so the stored name is still the old one.
    [Fact]
    public void RenameByNameLinks_CarriesTheNewNameOntoWhatLinksToIt()
    {
        var rename = _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Science Fiction");

        Assert.Equal("Sci-Fi", rename.PreviousName);
        Assert.Equal(_movieId, Assert.Single(rename.ItemIds));
        Assert.Equal("Science Fiction|Noir", StoredNames(_movieId).Genres);
    }

    [Fact]
    public void RenameByNameLinks_RenamesTheGenreItselfInTheSameBreath()
    {
        // Both ends move together: a genre left under its old name while the items carry the new one
        // would resolve that new spelling to a second genre on the next save of any of them.
        _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Science Fiction");

        using var ctx = CreateDbContext();
        var genre = ctx.BaseItems.AsNoTracking().Single(e => e.Id.Equals(_genreId));

        Assert.Equal("Science Fiction", genre.Name);
        Assert.Equal("Science Fiction".GetCleanValue(), genre.CleanName);
    }

    [Fact]
    public void RenameByNameLinks_ARenamedGenreStillResolvesFromItsNewName()
    {
        _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Science Fiction");

        // Both halves of the rename landed, so the item is found by the name it now carries and not
        // by the one it used to.
        Assert.Single(_repository.GetItemList(new InternalItemsQuery { Genres = ["Science Fiction"] }));
        Assert.Empty(_repository.GetItemList(new InternalItemsQuery { Genres = ["Sci-Fi"] }));
    }

    [Fact]
    public void RenameByNameLinks_LeavesAnItemThatOnlyNamesItAlone()
    {
        // Alien carries the same spelling but links to nothing, so it is not this genre's to rewrite.
        var rename = _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Science Fiction");

        Assert.DoesNotContain(_otherMovieId, rename.ItemIds);
        Assert.Equal("Sci-Fi", StoredNames(_otherMovieId).Genres);
    }

    [Fact]
    public void RenameByNameLinks_RewritesTheStudioNameToo()
    {
        var rename = _repository.RenameByNameLinks(_studioId, BaseItemKind.Studio, "Warner Brothers");

        Assert.Equal(_movieId, Assert.Single(rename.ItemIds));
        Assert.Equal("Warner Brothers", StoredNames(_movieId).Studios);
        Assert.Equal("Sci-Fi|Noir", StoredNames(_movieId).Genres);
    }

    [Fact]
    public void RenameByNameLinks_UnchangedName_TouchesNothing()
    {
        var rename = _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Sci-Fi");

        Assert.Null(rename.PreviousName);
        Assert.Empty(rename.ItemIds);
        Assert.Equal("Sci-Fi|Noir", StoredNames(_movieId).Genres);
    }

    [Fact]
    public void RenameByNameLinks_AnItemThatIsGone_ChangesNothing()
    {
        var rename = _repository.RenameByNameLinks(Guid.NewGuid(), BaseItemKind.Genre, "Science Fiction");

        Assert.Null(rename.PreviousName);
        Assert.Equal("Sci-Fi|Noir", StoredNames(_movieId).Genres);
    }

    [Fact]
    public void RenameByNameLinks_OntoASpellingTheItemAlreadyCarries_KeepsOneOfIt()
    {
        // The item names both, so folding one onto the other must not leave it holding a duplicate.
        _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Noir");

        Assert.Equal("Noir", StoredNames(_movieId).Genres);
    }

    [Fact]
    public void RenameByNameLinks_ANameAnotherOneMerelyContains_IsLeftAlone()
    {
        // "Sci-Fi Horror" contains the name being renamed but is not it, so a substring rewrite of the
        // stored column would corrupt it.
        using (var ctx = CreateDbContext())
        {
            var movie = ctx.BaseItems.Single(e => e.Id.Equals(_movieId));
            movie.Genres = "Sci-Fi|Sci-Fi Horror";
            ctx.SaveChanges();
        }

        _repository.RenameByNameLinks(_genreId, BaseItemKind.Genre, "Science Fiction");

        Assert.Equal("Science Fiction|Sci-Fi Horror", StoredNames(_movieId).Genres);
    }

    private void Rename(Guid id, string name)
    {
        using var ctx = CreateDbContext();
        var item = ctx.BaseItems.Single(e => e.Id.Equals(id));
        item.Name = name;
        item.CleanName = name.GetCleanValue();
        ctx.SaveChanges();
    }

    private void AddItem(JellyfinDbContext ctx, Guid id, BaseItemKind kind, string name, string? genres = null, string? studios = null)
    {
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            Name = name,
            CleanName = name.GetCleanValue(),
            Genres = genres,
            Studios = studios,
            IsFolder = false,
            IsVirtualItem = false
        });
    }

    private (string? Genres, string? Studios) StoredNames(Guid id)
    {
        using var ctx = CreateDbContext();
        var item = ctx.BaseItems.AsNoTracking().Single(e => e.Id.Equals(id));

        return (item.Genres, item.Studios);
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
