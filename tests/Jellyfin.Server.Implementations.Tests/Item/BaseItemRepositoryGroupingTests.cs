using System;
using System.Collections.Generic;
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
    private readonly string _boxSetTypeName;

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
        _boxSetTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.BoxSet];

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

    [Fact]
    public void GetItemList_BoxSetCollapse_NonNestedShowsEachBoxSet()
    {
        // Two independent collections, no nesting: the fast (non-materialized) path must keep both.
        var boxSetA = Guid.NewGuid();
        var boxSetC = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var m3 = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateBoxSetEntity(boxSetA, "Collection A"));
            ctx.BaseItems.Add(CreateBoxSetEntity(boxSetC, "Collection C"));
            ctx.BaseItems.Add(CreateMovie(m1));
            ctx.BaseItems.Add(CreateMovie(m2));
            ctx.BaseItems.Add(CreateMovie(m3));
            AddManualLink(ctx, boxSetA, m1);
            AddManualLink(ctx, boxSetA, m2);
            AddManualLink(ctx, boxSetC, m3);
            ctx.SaveChanges();
        }

        var ids = _repository.GetItemList(CreateCollapseQuery()).Select(i => i.Id).ToHashSet();

        Assert.Equal(new HashSet<Guid> { boxSetA, boxSetC }, ids);
    }

    [Fact]
    public void GetItemList_BoxSetCollapse_KeepsMoviesNotInAnyBoxSet()
    {
        var boxSetA = Guid.NewGuid();
        var inBoxSet = Guid.NewGuid();
        var standalone = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateBoxSetEntity(boxSetA, "Collection A"));
            ctx.BaseItems.Add(CreateMovie(inBoxSet));
            ctx.BaseItems.Add(CreateMovie(standalone));
            AddManualLink(ctx, boxSetA, inBoxSet);
            ctx.SaveChanges();
        }

        var ids = _repository.GetItemList(CreateCollapseQuery()).Select(i => i.Id).ToHashSet();

        // The grouped movie is hidden behind its collection; the standalone movie stays visible.
        Assert.Equal(new HashSet<Guid> { boxSetA, standalone }, ids);
    }

    [Fact]
    public void GetItemList_BoxSetCollapse_NestedBoxSetRollsUpToRoot()
    {
        // Outer contains two movies plus the inner collection; inner contains two more movies.
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var m3 = Guid.NewGuid();
        var m4 = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateBoxSetEntity(outer, "Outer"));
            ctx.BaseItems.Add(CreateBoxSetEntity(inner, "Inner"));
            ctx.BaseItems.Add(CreateMovie(m1));
            ctx.BaseItems.Add(CreateMovie(m2));
            ctx.BaseItems.Add(CreateMovie(m3));
            ctx.BaseItems.Add(CreateMovie(m4));
            AddManualLink(ctx, outer, m1);
            AddManualLink(ctx, outer, m2);
            AddManualLink(ctx, outer, inner);
            AddManualLink(ctx, inner, m3);
            AddManualLink(ctx, inner, m4);
            ctx.SaveChanges();
        }

        var ids = _repository.GetItemList(CreateCollapseQuery()).Select(i => i.Id).ToHashSet();

        // Only the outermost collection surfaces; the nested collection and all movies are hidden.
        Assert.Equal(new HashSet<Guid> { outer }, ids);
    }

    [Fact]
    public void GetItemList_BoxSetCollapse_PureNestingSurfacesOuterNotInner()
    {
        // Outer contains only the inner collection, which holds the movies. The outer collection
        // has no direct movie of its own, yet it must still be the one that surfaces.
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateBoxSetEntity(outer, "Outer"));
            ctx.BaseItems.Add(CreateBoxSetEntity(inner, "Inner"));
            ctx.BaseItems.Add(CreateMovie(m1));
            ctx.BaseItems.Add(CreateMovie(m2));
            AddManualLink(ctx, outer, inner);
            AddManualLink(ctx, inner, m1);
            AddManualLink(ctx, inner, m2);
            ctx.SaveChanges();
        }

        var ids = _repository.GetItemList(CreateCollapseQuery()).Select(i => i.Id).ToHashSet();

        Assert.Equal(new HashSet<Guid> { outer }, ids);
    }

    [Fact]
    public void GetItemList_BoxSetCollapse_TypeRestricted_NestedBoxSetRollsUpToRoot()
    {
        // Same nesting, but only Movies are collapsible (the per-type collapse path).
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateBoxSetEntity(outer, "Outer"));
            ctx.BaseItems.Add(CreateBoxSetEntity(inner, "Inner"));
            ctx.BaseItems.Add(CreateMovie(m1));
            ctx.BaseItems.Add(CreateMovie(m2));
            AddManualLink(ctx, outer, inner);
            AddManualLink(ctx, inner, m1);
            AddManualLink(ctx, inner, m2);
            ctx.SaveChanges();
        }

        var query = CreateCollapseQuery(collapseTypes: [BaseItemKind.Movie]);
        var ids = _repository.GetItemList(query).Select(i => i.Id).ToHashSet();

        Assert.Equal(new HashSet<Guid> { outer }, ids);
    }

    [Fact]
    public void GetItemList_CollectionsLibrary_ShowsAllBoxSetsWithoutRollUp()
    {
        // Querying the collections library itself (BoxSet is the requested type) must never roll
        // up: both the outer and the nested collection stay visible.
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateBoxSetEntity(outer, "Outer"));
            ctx.BaseItems.Add(CreateBoxSetEntity(inner, "Inner"));
            ctx.BaseItems.Add(CreateMovie(m1));
            ctx.BaseItems.Add(CreateMovie(m2));
            AddManualLink(ctx, outer, inner);
            AddManualLink(ctx, inner, m1);
            AddManualLink(ctx, inner, m2);
            ctx.SaveChanges();
        }

        var query = CreateCollapseQuery(includeTypes: [BaseItemKind.BoxSet]);
        var ids = _repository.GetItemList(query).Select(i => i.Id).ToHashSet();

        Assert.Equal(new HashSet<Guid> { outer, inner }, ids);
    }

    private InternalItemsQuery CreateCollapseQuery(BaseItemKind[]? collapseTypes = null, BaseItemKind[]? includeTypes = null)
    {
        return new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = includeTypes ?? [BaseItemKind.Movie],
            CollapseBoxSetItems = true,
            CollapseBoxSetItemTypes = collapseTypes ?? [],
            // Isolate the BoxSet collapse from the alternate-version presentation grouping.
            GroupByPresentationUniqueKey = false
        };
    }

    private BaseItemEntity CreateMovie(Guid id)
    {
        return CreateMovieEntity(id, "Movie " + id.ToString("N"), id.ToString("N"), null);
    }

    private BaseItemEntity CreateBoxSetEntity(Guid id, string name)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _boxSetTypeName,
            Name = name,
            PresentationUniqueKey = id.ToString("N"),
            MediaType = "Unknown",
            IsFolder = true,
            IsVirtualItem = false
        };
    }

    private static void AddManualLink(JellyfinDbContext context, Guid parentId, Guid childId)
    {
        // (ParentId, SortOrder) is the primary key, so every link of a parent needs its own order.
        var sortOrder = context.LinkedChildren.Local.Count(l => l.ParentId.Equals(parentId));

        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = parentId,
            ChildId = childId,
            SortOrder = sortOrder,
            ChildType = Database.Implementations.Entities.LinkedChildType.Manual
        });
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
