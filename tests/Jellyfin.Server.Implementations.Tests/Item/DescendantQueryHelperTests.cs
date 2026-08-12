using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.MatchCriteria;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies the descendant traversals against the SQLite provider: the sets they resolve, and that
/// they stay sub-selects instead of inlining every descendant id into the statement.
/// </summary>
public sealed class DescendantQueryHelperTests : SqliteDbTestFixture
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly Dictionary<Guid, int> _linkCounters = new();

    public DescendantQueryHelperTests()
    {
    }

    [Fact]
    public void GetAllDescendantIds_Hierarchy_ReturnsEveryLevelWithoutTheParent()
    {
        var library = Guid.NewGuid();
        var series = Guid.NewGuid();
        var season = Guid.NewGuid();
        var episode = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddFolder(ctx, library);
            AddFolder(ctx, series);
            AddFolder(ctx, season);
            AddItem(ctx, episode, MovieType);

            // AncestorIds is a closure: production writes one row per ancestor, not just the parent.
            AddAncestors(ctx, series, library);
            AddAncestors(ctx, season, series, library);
            AddAncestors(ctx, episode, season, series, library);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var descendants = DescendantQueryHelper.GetAllDescendantIds(ctx, library).ToHashSet();

            Assert.Equal(new[] { series, season, episode }.Order(), descendants.Order());
            Assert.DoesNotContain(library, descendants);
        }
    }

    [Fact]
    public void GetAllDescendantIds_LinkedFolder_IncludesItsOwnDescendants()
    {
        var boxSet = Guid.NewGuid();
        var series = Guid.NewGuid();
        var episode = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, boxSet, BoxSetType, isFolder: true);
            AddFolder(ctx, series);
            AddItem(ctx, episode, MovieType);

            AddAncestors(ctx, episode, series);
            AddLink(ctx, boxSet, series);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var descendants = DescendantQueryHelper.GetAllDescendantIds(ctx, boxSet).ToHashSet();

            Assert.Contains(series, descendants);
            Assert.Contains(episode, descendants);
        }
    }

    // Timeout so that a missing termination guard fails the test instead of hanging the run.
    [Fact(Timeout = 30000)]
    public void GetAllDescendantIds_NestedLinks_AreFollowedAndCyclesTerminate()
    {
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        var movie = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, outer, BoxSetType, isFolder: true);
            AddItem(ctx, inner, BoxSetType, isFolder: true);
            AddItem(ctx, movie, MovieType);

            AddLink(ctx, outer, inner);
            AddLink(ctx, inner, movie);
            // The traversal must not spin on this cycle.
            AddLink(ctx, inner, outer);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var descendants = DescendantQueryHelper.GetAllDescendantIds(ctx, outer).ToHashSet();

            Assert.Contains(inner, descendants);
            Assert.Contains(movie, descendants);
            Assert.DoesNotContain(outer, descendants);
        }
    }

    [Fact]
    public void GetAllDescendantIds_LinksOfNonFolders_AreNotFollowed()
    {
        var library = Guid.NewGuid();
        var movie = Guid.NewGuid();
        var alternateVersion = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddFolder(ctx, library);
            AddItem(ctx, movie, MovieType);
            AddItem(ctx, alternateVersion, MovieType);

            AddAncestors(ctx, movie, library);
            // An alternate version hangs off the movie by link, and the movie is not a folder.
            AddLink(ctx, movie, alternateVersion);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var descendants = DescendantQueryHelper.GetAllDescendantIds(ctx, library).ToHashSet();

            Assert.Contains(movie, descendants);
            Assert.DoesNotContain(alternateVersion, descendants);
        }
    }

    [Fact]
    public void GetAllDescendantIds_ClosureSeamAboveTheCollectionFolder_IsCrossed()
    {
        var userRoot = Guid.NewGuid();
        var collectionFolder = Guid.NewGuid();
        var series = Guid.NewGuid();
        var episode = Guid.NewGuid();
        var boxSet = Guid.NewGuid();
        var linkedMovie = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddFolder(ctx, userRoot);
            AddFolder(ctx, collectionFolder);
            AddFolder(ctx, series);
            AddItem(ctx, episode, MovieType);
            AddItem(ctx, boxSet, BoxSetType, isFolder: true);
            AddItem(ctx, linkedMovie, MovieType);

            // An item carries its own chain plus its collection folder, but not the user root above
            // it, so one hop from the user root stops at the collection folder.
            AddAncestors(ctx, collectionFolder, userRoot);
            AddAncestors(ctx, series, collectionFolder);
            AddAncestors(ctx, episode, series, collectionFolder);
            AddAncestors(ctx, boxSet, collectionFolder);
            // The box set is only reachable across the seam, and its links have to be followed too.
            AddLink(ctx, boxSet, linkedMovie);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var descendants = DescendantQueryHelper.GetAllDescendantIds(ctx, userRoot).ToHashSet();

            Assert.Equal(
                new[] { collectionFolder, series, episode, boxSet, linkedMovie }.Order(),
                descendants.Order());
        }
    }

    [Fact]
    public void GetOwnedDescendantIds_ClosureSeamAboveTheCollectionFolder_IsCrossed()
    {
        var userRoot = Guid.NewGuid();
        var collectionFolder = Guid.NewGuid();
        var series = Guid.NewGuid();
        var episode = Guid.NewGuid();
        var boxSet = Guid.NewGuid();
        var linkedMovie = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddFolder(ctx, userRoot);
            AddFolder(ctx, collectionFolder);
            AddFolder(ctx, series);
            AddItem(ctx, episode, MovieType);
            AddItem(ctx, boxSet, BoxSetType, isFolder: true);
            AddItem(ctx, linkedMovie, MovieType);

            AddAncestors(ctx, collectionFolder, userRoot);
            AddAncestors(ctx, series, collectionFolder);
            AddAncestors(ctx, episode, series, collectionFolder);
            AddAncestors(ctx, boxSet, collectionFolder);
            AddLink(ctx, boxSet, linkedMovie);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            // Owned only: the linked movie stays out, or deleting a library would delete it.
            var expected = new[] { collectionFolder, series, episode, boxSet }.Order();

            Assert.Equal(expected, DescendantQueryHelper.GetOwnedDescendantIds(ctx, userRoot).ToHashSet().Order());
            Assert.Equal(expected, DescendantQueryHelper.GetOwnedDescendantIdsBatch(ctx, [userRoot]).Order());
        }
    }

    [Fact]
    public void GetFolderIdsMatching_LinkAboveAClosure_ReturnsTheLinkingFolder()
    {
        var collections = Guid.NewGuid();
        var boxSet = Guid.NewGuid();
        var library = Guid.NewGuid();
        var series = Guid.NewGuid();
        var episode = Guid.NewGuid();
        var otherLibrary = Guid.NewGuid();
        var otherBoxSet = Guid.NewGuid();
        var silentMovie = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddFolder(ctx, collections);
            AddItem(ctx, boxSet, BoxSetType, isFolder: true);
            AddFolder(ctx, library);
            AddFolder(ctx, series);
            AddItem(ctx, episode, MovieType);

            AddAncestors(ctx, boxSet, collections);
            AddAncestors(ctx, series, library);
            AddAncestors(ctx, episode, series, library);
            // The link lands on the series, not on the episode that carries the subtitles.
            AddLink(ctx, boxSet, series);
            AddStream(ctx, episode, MediaStreamTypeEntity.Subtitle);

            AddFolder(ctx, otherLibrary);
            AddItem(ctx, otherBoxSet, BoxSetType, isFolder: true);
            AddItem(ctx, silentMovie, MovieType);
            AddAncestors(ctx, otherBoxSet, collections);
            AddAncestors(ctx, silentMovie, otherLibrary);
            AddLink(ctx, otherBoxSet, silentMovie);
            // A stream of another type: the criteria, not the mere presence of a stream, decides.
            AddStream(ctx, silentMovie, MediaStreamTypeEntity.Video);

            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var folders = DescendantQueryHelper.GetFolderIdsMatching(ctx, new HasSubtitles()).ToHashSet();

            Assert.Equal(new[] { library, series, boxSet, collections }.Order(), folders.Order());
        }
    }

    [Fact(Timeout = 30000)]
    public void GetFolderIdsMatching_NestedLinks_AreFollowedAndCyclesTerminate()
    {
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        var movie = Guid.NewGuid();
        var silentSet = Guid.NewGuid();
        var silentMovie = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, outer, BoxSetType, isFolder: true);
            AddItem(ctx, inner, BoxSetType, isFolder: true);
            AddItem(ctx, movie, MovieType);

            AddLink(ctx, outer, inner);
            AddLink(ctx, inner, movie);
            // Resolving the link parents must not spin on this cycle.
            AddLink(ctx, inner, outer);
            AddStream(ctx, movie, MediaStreamTypeEntity.Subtitle);

            AddItem(ctx, silentSet, BoxSetType, isFolder: true);
            AddItem(ctx, silentMovie, MovieType);
            AddLink(ctx, silentSet, silentMovie);
            AddStream(ctx, silentMovie, MediaStreamTypeEntity.Video);

            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var folders = DescendantQueryHelper.GetFolderIdsMatching(ctx, new HasSubtitles()).ToHashSet();

            Assert.Equal(new[] { inner, outer }.Order(), folders.Order());
        }
    }

    [Fact]
    public void GetFolderIdsMatching_ClosureSeamAboveTheCollectionFolder_IsCrossed()
    {
        var userRoot = Guid.NewGuid();
        var collectionFolder = Guid.NewGuid();
        var series = Guid.NewGuid();
        var episode = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddFolder(ctx, userRoot);
            AddFolder(ctx, collectionFolder);
            AddFolder(ctx, series);
            AddItem(ctx, episode, MovieType);

            // The closure is not transitive at this seam: no item records the user root.
            AddAncestors(ctx, episode, series, collectionFolder);
            AddAncestors(ctx, series, collectionFolder);
            AddAncestors(ctx, collectionFolder, userRoot);
            AddStream(ctx, episode, MediaStreamTypeEntity.Subtitle);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var folders = DescendantQueryHelper.GetFolderIdsMatching(ctx, new HasSubtitles()).ToHashSet();

            Assert.Equal(new[] { series, collectionFolder, userRoot }.Order(), folders.Order());
        }
    }

    [Fact]
    public void GetFolderIdsMatching_LinkedFolder_MatchesOnLanguageOnly()
    {
        var boxSet = Guid.NewGuid();
        var series = Guid.NewGuid();
        var episode = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, boxSet, BoxSetType, isFolder: true);
            AddFolder(ctx, series);
            AddItem(ctx, episode, MovieType);

            AddAncestors(ctx, episode, series);
            AddLink(ctx, boxSet, series);
            AddStream(ctx, episode, MediaStreamTypeEntity.Subtitle, "ger");
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            var german = new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, ["ger"]);
            var french = new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, ["fre"]);

            Assert.Equal(new[] { series, boxSet }.Order(), DescendantQueryHelper.GetFolderIdsMatching(ctx, german).ToHashSet().Order());
            Assert.Empty(DescendantQueryHelper.GetFolderIdsMatching(ctx, french).ToArray());
        }
    }

    [Fact]
    public void GetOwnedDescendantIds_IgnoresLinkedChildren()
    {
        var boxSet = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var linked = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            AddItem(ctx, boxSet, BoxSetType, isFolder: true);
            AddItem(ctx, owned, MovieType);
            AddItem(ctx, linked, MovieType);

            AddAncestors(ctx, owned, boxSet);
            AddLink(ctx, boxSet, linked);
            ctx.SaveChanges();
        }

        using (var ctx = CreateDbContext())
        {
            Assert.Equal([owned], DescendantQueryHelper.GetOwnedDescendantIds(ctx, boxSet).ToArray());
            Assert.Equal([owned], DescendantQueryHelper.GetOwnedDescendantIdsBatch(ctx, [boxSet]).ToArray());
        }
    }

    [Fact]
    public void GetAllDescendantIds_StatementSizeDoesNotGrowWithTheLibrary()
    {
        var small = SeedLibrary(10);
        var large = SeedLibrary(500);

        using var ctx = CreateDbContext();

        var smallSql = CountingQuery(ctx, small).ToQueryString();
        var largeSql = CountingQuery(ctx, large).ToQueryString();

        // Reading the ids into memory and handing them back as AsQueryable() makes EF inline one
        // literal per descendant, which is what allocated megabytes per call.
        Assert.Equal(smallSql.Length, largeSql.Length);
        Assert.Contains("AncestorIds", smallSql, StringComparison.Ordinal);
        Assert.Equal(10, CountingQuery(ctx, small).Count());
        Assert.Equal(500, CountingQuery(ctx, large).Count());
    }

    private static IQueryable<BaseItemEntity> CountingQuery(JellyfinDbContext context, Guid libraryId)
    {
        var descendantIds = DescendantQueryHelper.GetAllDescendantIds(context, libraryId);

        return context.BaseItems
            .AsNoTracking()
            .Where(b => descendantIds.Contains(b.Id))
            .Where(DescendantQueryHelper.IsCountableLeaf);
    }

    private Guid SeedLibrary(int childCount)
    {
        var library = Guid.NewGuid();

        using var ctx = CreateDbContext();
        AddFolder(ctx, library);
        for (var i = 0; i < childCount; i++)
        {
            var child = Guid.NewGuid();
            AddItem(ctx, child, MovieType);
            AddAncestors(ctx, child, library);
        }

        ctx.SaveChanges();

        return library;
    }

    private static void AddFolder(JellyfinDbContext context, Guid id)
        => AddItem(context, id, FolderType, isFolder: true);

    private static void AddItem(JellyfinDbContext context, Guid id, string type, bool isFolder = false)
        => context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = type,
            Name = type + " " + id,
            IsFolder = isFolder
        });

    private static void AddStream(JellyfinDbContext context, Guid itemId, MediaStreamTypeEntity type, string? language = null)
        => context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = itemId,
            StreamIndex = 0,
            StreamType = type,
            Language = language,
            Item = null!
        });

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

    // LinkedChildren is keyed on (ParentId, SortOrder), so every link of a parent needs its own slot.
    private void AddLink(JellyfinDbContext context, Guid parentId, Guid childId)
    {
        _linkCounters.TryGetValue(parentId, out var sortOrder);
        _linkCounters[parentId] = sortOrder + 1;

        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = parentId,
            ChildId = childId,
            ChildType = LinkedChildType.Manual,
            SortOrder = sortOrder
        });
    }
}
