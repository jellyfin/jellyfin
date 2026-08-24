using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryGroupingTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly string _movieTypeName;

    public BaseItemRepositoryGroupingTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];

        _repository = CreateBaseItemRepository(itemTypeLookup);
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
}
