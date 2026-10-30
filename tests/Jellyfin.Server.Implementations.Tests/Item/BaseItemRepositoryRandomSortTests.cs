using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers <see cref="InternalItemsQuery.RandomSeed"/>: a client paging through a random sort sends the
/// same seed with every page, so the pages have to come from one shuffle instead of a new one each.
/// </summary>
public sealed class BaseItemRepositoryRandomSortTests : SqliteDbTestFixture
{
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";
    private const int MovieCount = 40;
    private const int PageSize = 10;

    private readonly BaseItemRepository _repository;
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    public BaseItemRepositoryRandomSortTests()
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            for (var i = 0; i < MovieCount; i++)
            {
                AddMovie(context, $"Movie {i:D2}");
            }

            context.SaveChanges();
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void RandomWithSeed_PagesAddUpToTheWholeOrder()
    {
        var pagedIds = new List<Guid>();
        for (var startIndex = 0; startIndex < MovieCount; startIndex += PageSize)
        {
            var page = _repository.GetItems(Query(seed: 42, startIndex, PageSize));

            Assert.Equal(MovieCount, page.TotalRecordCount);
            pagedIds.AddRange(page.Items.Select(i => i.Id));
        }

        Assert.Equal(MovieCount, pagedIds.Distinct().Count());
        Assert.Equal(AllIds(seed: 42), pagedIds);
    }

    [Fact]
    public void RandomWithSeed_SameSeedRepeatsTheOrder()
    {
        Assert.Equal(AllIds(seed: 42), AllIds(seed: 42));
    }

    [Fact]
    public void RandomWithSeed_OtherSeedShufflesDifferently()
    {
        Assert.NotEqual(AllIds(seed: 1), AllIds(seed: 2));
    }

    [Fact]
    public void RandomWithSeed_NewItemLeavesTheRestInOrder()
    {
        var before = AllIds(seed: 42);

        Guid added;
        using (var context = CreateDbContext())
        {
            added = AddMovie(context, "Movie added later");
            context.SaveChanges();
        }

        Assert.Equal(before, AllIds(seed: 42).Where(id => !id.Equals(added)));
    }

    private List<Guid> AllIds(int seed)
        => _repository
            .GetItemList(Query(seed, startIndex: null, limit: null))
            .Select(i => i.Id)
            .ToList();

    private InternalItemsQuery Query(int seed, int? startIndex, int? limit)
        => new(_user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)],
            RandomSeed = seed,
            StartIndex = startIndex,
            Limit = limit,
            EnableTotalRecordCount = true
        };

    private static Guid AddMovie(JellyfinDbContext context, string name)
    {
        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = MovieType, Name = name, SortName = name, PresentationUniqueKey = id.ToString("N") });
        return id;
    }
}
