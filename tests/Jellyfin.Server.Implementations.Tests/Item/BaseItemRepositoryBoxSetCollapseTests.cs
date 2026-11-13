using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers collapsing movies into the collections they belong to when the collection links a version
/// that has since been merged behind another one: the merged version is not listed itself, so the
/// collection has to be found through the primary.
/// </summary>
public sealed class BaseItemRepositoryBoxSetCollapseTests : SqliteDbTestFixture
{
    private static readonly Guid _folderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _primaryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _mergedVersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _boxSetId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid _otherMovieId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private readonly BaseItemRepository _repository;

    public BaseItemRepositoryBoxSetCollapseTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(itemTypeLookup);

        var movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie]!;
        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = _folderId,
            Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder]!,
            Name = "Movies",
            Path = "/movies",
            IsFolder = true
        });
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = _boxSetId,
            Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.BoxSet]!,
            Name = "Blade Runner Collection",
            Path = "/collections/Blade Runner Collection",
            IsFolder = true
        });
        ctx.BaseItems.Add(CreateMovie(_primaryId, movieTypeName, "Blade Runner", "/movies 4k/Blade Runner (1982)/movie.mkv", null));
        ctx.BaseItems.Add(CreateMovie(_mergedVersionId, movieTypeName, "Blade Runner", "/movies/Blade Runner (1982)/movie.mkv", _primaryId));
        ctx.BaseItems.Add(CreateMovie(_otherMovieId, movieTypeName, "Alien", "/movies/Alien (1979)/movie.mkv", null));
        ctx.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _boxSetId,
            ChildId = _mergedVersionId,
            ChildType = LinkedChildType.Manual
        });
        ctx.SaveChanges();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetItemList_CollectionLinksAMergedVersion_CollapsesThePrimaryIntoIt(bool restrictToMovies)
    {
        var result = _repository.GetItemList(new InternalItemsQuery
        {
            ParentId = _folderId,
            CollapseBoxSetItems = true,
            CollapseBoxSetItemTypes = restrictToMovies ? [BaseItemKind.Movie] : []
        });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id.Equals(_boxSetId));
        Assert.Contains(result, i => i.Id.Equals(_otherMovieId));
    }

    private static BaseItemEntity CreateMovie(Guid id, string typeName, string name, string path, Guid? primaryVersionId)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = typeName,
            Name = name,
            Path = path,
            ParentId = _folderId,
            TopParentId = _folderId,
            PresentationUniqueKey = (primaryVersionId ?? id).ToString("N"),
            PrimaryVersionId = primaryVersionId,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
