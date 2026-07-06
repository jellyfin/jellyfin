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
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryGroupingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly BaseItemRepository _repository;
    private readonly string _movieTypeName;

    public BaseItemRepositoryGroupingTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

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
    public void GetItemList_VersionGroup_ReturnsPrimaryVersion()
    {
        // The alternate version sorts before the primary by id, so a plain Min(Id) per
        // presentation key would wrongly pick the alternate as the group representative.
        var primaryId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var alternateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var presentationKey = primaryId.ToString("N");

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(primaryId, "Movie", presentationKey, null));
            ctx.BaseItems.Add(CreateMovieEntity(alternateId, "Movie - 1080p", presentationKey, primaryId));
            ctx.SaveChanges();
        }

        var result = _repository.GetItemList(CreateQuery());

        var item = Assert.Single(result);
        Assert.Equal(primaryId, item.Id);
    }

    [Fact]
    public void GetItemList_GroupWithoutPrimary_FallsBackToMinId()
    {
        var firstId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var secondId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var otherPrimaryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var presentationKey = otherPrimaryId.ToString("N");

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(firstId, "Movie", presentationKey, otherPrimaryId));
            ctx.BaseItems.Add(CreateMovieEntity(secondId, "Movie - 4K", presentationKey, otherPrimaryId));
            ctx.SaveChanges();
        }

        var result = _repository.GetItemList(CreateQuery());

        var item = Assert.Single(result);
        Assert.Equal(firstId, item.Id);
    }

    private static InternalItemsQuery CreateQuery()
    {
        // IncludeOwnedItems keeps the alternate version rows in the query so the
        // grouping collapse is what picks the group representative.
        return new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IncludeOwnedItems = true
        };
    }

    private BaseItemEntity CreateMovieEntity(Guid id, string name, string presentationKey, Guid? primaryVersionId)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            PresentationUniqueKey = presentationKey,
            PrimaryVersionId = primaryVersionId,
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
