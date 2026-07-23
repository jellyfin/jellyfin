using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class OriginalTitleSearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly BaseItemRepository _repository;
    private readonly string _movieTypeName;

    public OriginalTitleSearchTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _connection.CreateFunction("UnicodeLower", (string? s) => s?.ToLowerInvariant(), isDeterministic: true);

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            itemTypeLookup,
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void GetItemList_SearchByOriginalTitle_NonAscii_ReturnsMatch()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Spirited Away", "千と千尋の神隠し"));
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "My Neighbor Totoro", "となりのトトロ"));
            ctx.SaveChanges();
        }

        var results = _repository.GetItemList(CreateSearchQuery("千と千尋"));

        Assert.Single(results);
        Assert.Equal("Spirited Away", results[0].Name);
    }

    [Fact]
    public void GetItemList_SearchByOriginalTitle_AsciiCaseInsensitive_ReturnsMatch()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "My Movie", "Spirited Away"));
            ctx.SaveChanges();
        }

        var results = _repository.GetItemList(CreateSearchQuery("spirited away"));

        Assert.Single(results);
        Assert.Equal("My Movie", results[0].Name);
    }

    [Fact]
    public void GetItemList_SearchByOriginalTitle_CyrillicCaseInsensitive_ReturnsMatch()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "The Expendables", "Неудержимые"));
            ctx.SaveChanges();
        }

        var results = _repository.GetItemList(CreateSearchQuery("неудержимые"));

        Assert.Single(results);
        Assert.Equal("The Expendables", results[0].Name);
    }

    [Fact]
    public void GetItemList_SearchByName_OriginalTitleNotMatching_ReturnsNameMatch()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Spirited Away", "千と千尋の神隠し"));
            ctx.SaveChanges();
        }

        var results = _repository.GetItemList(CreateSearchQuery("Spirited Away"));

        Assert.Single(results);
    }

    [Fact]
    public void GetItemList_SearchTerm_NoMatch_ReturnsEmpty()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Spirited Away", "千と千尋の神隠し"));
            ctx.SaveChanges();
        }

        var results = _repository.GetItemList(CreateSearchQuery("Totoro"));

        Assert.Empty(results);
    }

    private static InternalItemsQuery CreateSearchQuery(string searchTerm)
    {
        return new InternalItemsQuery
        {
            SearchTerm = searchTerm,
            IncludeItemTypes = [BaseItemKind.Movie]
        };
    }

    private BaseItemEntity CreateMovieEntity(Guid id, string name, string originalTitle)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            CleanName = name.ToLowerInvariant(),
            OriginalTitle = originalTitle,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
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
