using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Xunit;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class LinkedChildrenServiceVersionTests : SqliteDbTestFixture
{
    private static readonly Guid _moviePrimaryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _movieAutoLinkedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _movieLinkedId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _movieLocalId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid _episodePrimaryId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid _episodeAutoLinkedId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private readonly LinkedChildrenService _service;

    public LinkedChildrenServiceVersionTests()
    {
        var lookup = new ItemTypeLookup();
        _service = new LinkedChildrenService(CreateDbContextFactory(), lookup, CreateBaseItemRepository(lookup));

        var movieType = lookup.BaseItemKindNames[BaseItemKind.Movie]!;
        var episodeType = lookup.BaseItemKindNames[BaseItemKind.Episode]!;
        using var context = CreateDbContext();
        foreach (var id in new[] { _moviePrimaryId, _movieAutoLinkedId, _movieLinkedId, _movieLocalId })
        {
            context.BaseItems.Add(new BaseItemEntity { Id = id, Type = movieType, Name = "Blade Runner" });
        }

        foreach (var id in new[] { _episodePrimaryId, _episodeAutoLinkedId })
        {
            context.BaseItems.Add(new BaseItemEntity { Id = id, Type = episodeType, Name = "Pilot" });
        }

        context.LinkedChildren.AddRange(
            new LinkedChildEntity { ParentId = _moviePrimaryId, ChildId = _movieLocalId, ChildType = DbLinkedChildType.LocalAlternateVersion, SortOrder = 0 },
            new LinkedChildEntity { ParentId = _moviePrimaryId, ChildId = _movieLinkedId, ChildType = DbLinkedChildType.LinkedAlternateVersion, SortOrder = 1 },
            new LinkedChildEntity { ParentId = _moviePrimaryId, ChildId = _movieAutoLinkedId, ChildType = DbLinkedChildType.AutoLinkedAlternateVersion, SortOrder = 2 },
            new LinkedChildEntity { ParentId = _episodePrimaryId, ChildId = _episodeAutoLinkedId, ChildType = DbLinkedChildType.AutoLinkedAlternateVersion, SortOrder = 0 });
        context.SaveChanges();
    }

    [Fact]
    public void GetLinkedChildrenIds_SeveralTypes_ReturnsOnlyThoseInSortOrder()
    {
        var ids = _service.GetLinkedChildrenIds(_moviePrimaryId, [LinkedChildType.LinkedAlternateVersion, LinkedChildType.AutoLinkedAlternateVersion]);

        Assert.Equal([_movieLinkedId, _movieAutoLinkedId], ids);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie)]
    [InlineData(BaseItemKind.Episode)]
    public void GetParentIdsWithChildType_ParentType_ReturnsOnlyParentsOfThatKind(BaseItemKind parentType)
    {
        var ids = _service.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion, parentType);

        Assert.Equal(parentType == BaseItemKind.Movie ? _moviePrimaryId : _episodePrimaryId, Assert.Single(ids));
    }

    [Fact]
    public void GetParentIdsWithChildType_NoParentType_ReturnsEveryParent()
    {
        var ids = _service.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion);

        Assert.Equal(2, ids.Count);
    }
}
