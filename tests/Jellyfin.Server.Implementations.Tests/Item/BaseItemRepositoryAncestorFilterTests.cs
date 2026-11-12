using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers <see cref="InternalItemsQuery.AncestorIds"/>, the filter every recursive query under a parent runs on.
/// </summary>
public sealed class BaseItemRepositoryAncestorFilterTests : SqliteDbTestFixture
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string SeasonType = "MediaBrowser.Controller.Entities.TV.Season";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private const string SeasonKey = "series-key-001";

    private readonly QueryPlanRecorder _recorder;
    private readonly BaseItemRepository _repository;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _series = Guid.NewGuid();
    private readonly Guid _season = Guid.NewGuid();
    private readonly Guid _episode = Guid.NewGuid();
    private readonly Guid _otherSeries = Guid.NewGuid();
    private readonly Guid _otherEpisode = Guid.NewGuid();
    private readonly Guid _emptyFolder = Guid.NewGuid();

    public BaseItemRepositoryAncestorFilterTests()
        : this(new QueryPlanRecorder())
    {
    }

    private BaseItemRepositoryAncestorFilterTests(QueryPlanRecorder recorder)
        : base(recorder)
    {
        _recorder = recorder;
        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void AncestorIds_SingleAncestor_ReturnsOnlyItsDescendants()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { AncestorIds = [_series] });

        Assert.Equal(new[] { _season, _episode }.Order(), ids.Order());
    }

    [Fact]
    public void AncestorIds_OverlappingAncestors_ReturnsUnionWithoutDuplicates()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            AncestorIds = [_library, _series, _otherSeries],
            IncludeItemTypes = [BaseItemKind.Episode]
        });

        Assert.Equal(new[] { _episode, _otherEpisode }.Order(), ids.Order());
    }

    [Fact]
    public void AncestorIds_ParentWithoutDescendants_ReturnsNothing()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { AncestorIds = [_emptyFolder] });

        Assert.Empty(ids);
    }

    [Fact]
    public void AncestorWithPresentationUniqueKey_ReturnsDescendantsOfTheKeyedFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            AncestorWithPresentationUniqueKey = SeasonKey,
            IncludeItemTypes = [BaseItemKind.Episode]
        });

        Assert.Equal([_episode], ids);
    }

    [Fact]
    public void AncestorIds_SeeksDescendantsByParent()
    {
        _recorder.Commands.Clear();

        _repository.GetItemList(new InternalItemsQuery
        {
            AncestorIds = [_emptyFolder],
            OrderBy = [(ItemSortBy.IsFolder, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending)],
            Limit = 1
        });

        var query = Assert.Single(_recorder.Commands, c => c.Sql.Contains("\"AncestorIds\"", StringComparison.Ordinal));
        AssertDrivenByDescendants(Explain(query));
    }

    [Fact]
    public void AncestorWithPresentationUniqueKey_SeeksDescendantsByParent()
    {
        _recorder.Commands.Clear();

        _repository.GetItemList(new InternalItemsQuery
        {
            AncestorWithPresentationUniqueKey = SeasonKey,
            OrderBy = [(ItemSortBy.IsFolder, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending)],
            Limit = 1
        });

        var query = Assert.Single(_recorder.Commands, c => c.Sql.Contains("\"AncestorIds\"", StringComparison.Ordinal));
        AssertDrivenByDescendants(Explain(query));
    }

    // The issue's plan scanned BaseItems and probed AncestorIds per row; the fix looks BaseItems up by the descendant ids.
    private static void AssertDrivenByDescendants(string[] plan)
    {
        Assert.Contains(plan, line => line.Contains("COVERING INDEX IX_AncestorIds_ParentItemId_ItemId (ParentItemId=?)", StringComparison.Ordinal));
        Assert.Contains(plan, line => line.StartsWith("SEARCH b USING INDEX sqlite_autoindex_BaseItems_1 (Id=?)", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, line => line.StartsWith("SCAN b ", StringComparison.Ordinal) || line == "SCAN b");
    }

    private void Seed(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _library, Type = FolderType, Name = "Shows", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _series, Type = SeriesType, Name = "Series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _season, Type = SeasonType, Name = "Season 1", IsFolder = true, PresentationUniqueKey = SeasonKey });
        context.BaseItems.Add(new BaseItemEntity { Id = _episode, Type = EpisodeType, Name = "Episode 1" });
        context.BaseItems.Add(new BaseItemEntity { Id = _otherSeries, Type = SeriesType, Name = "Other series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _otherEpisode, Type = EpisodeType, Name = "Other episode" });
        context.BaseItems.Add(new BaseItemEntity { Id = _emptyFolder, Type = FolderType, Name = "Empty", IsFolder = true });

        // AncestorIds is a closure: production writes one row per ancestor, not just the parent.
        AddAncestors(context, _series, _library);
        AddAncestors(context, _season, _series, _library);
        AddAncestors(context, _episode, _season, _series, _library);
        AddAncestors(context, _otherSeries, _library);
        AddAncestors(context, _otherEpisode, _otherSeries, _library);
        AddAncestors(context, _emptyFolder, _library);

        context.SaveChanges();
    }

    private static void AddAncestors(JellyfinDbContext context, Guid itemId, params Guid[] ancestorIds)
    {
        foreach (var ancestorId in ancestorIds)
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = ancestorId,
                Item = null!,
                ParentItem = null!
            });
        }
    }

    private string[] Explain(RecordedCommand query)
    {
        using var context = CreateDbContext();
        return QueryPlanRecorder.Explain(context, query);
    }
}
