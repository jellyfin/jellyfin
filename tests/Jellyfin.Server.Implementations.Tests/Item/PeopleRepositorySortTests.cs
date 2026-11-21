using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The person list sorts the way the artist list could. The Peoples table only knows a name, so every other
/// key has to come from the person's by-name item, and the name stays the tiebreaker so pages don't overlap.
/// </summary>
public sealed class PeopleRepositorySortTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _repository;
    private readonly User _user = new("sort-test", "provider", "reset");

    public PeopleRepositorySortTests()
    {
        var lookup = new ItemTypeLookup();
        var personType = lookup.BaseItemKindNames[BaseItemKind.Person];
        var favouriteId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);

            // Alphabetical, creation, birth and rating orders all differ so each key has its own expected order.
            context.BaseItems.Add(Person(Guid.NewGuid(), personType, "Alpha", created: 2024, born: 1970, rating: 5));
            context.BaseItems.Add(Person(Guid.NewGuid(), personType, "bravo", created: 2026, born: 1950, rating: 9));
            context.BaseItems.Add(Person(favouriteId, personType, "Charlie", created: 2025, born: 1990, rating: 7));

            foreach (var name in new[] { "Alpha", "bravo", "Charlie" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, PersonType = "Actor" });
            }

            context.UserData.Add(new UserData
            {
                ItemId = favouriteId,
                UserId = _user.Id,
                CustomDataKey = string.Empty,
                IsFavorite = true,
                Item = null,
                User = null
            });

            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), lookup, Mock.Of<IItemQueryHelpers>());
    }

    [Fact]
    public void GetPeople_NoSort_OrdersByNameIgnoringCase()
    {
        Assert.Equal(["Alpha", "bravo", "Charlie"], Names(new InternalPeopleQuery()));
    }

    [Fact]
    public void GetPeople_NameDescending_ReversesTheNameOrder()
    {
        Assert.Equal(["Charlie", "bravo", "Alpha"], Names(Sorted(ItemSortBy.SortName, SortOrder.Descending)));
    }

    [Fact]
    public void GetPeople_DateCreatedDescending_PutsTheNewestPersonFirst()
    {
        Assert.Equal(["bravo", "Charlie", "Alpha"], Names(Sorted(ItemSortBy.DateCreated, SortOrder.Descending)));
    }

    [Fact]
    public void GetPeople_PremiereDateAscending_OrdersByBirthDate()
    {
        Assert.Equal(["bravo", "Alpha", "Charlie"], Names(Sorted(ItemSortBy.PremiereDate, SortOrder.Ascending)));
    }

    [Fact]
    public void GetPeople_CommunityRatingDescending_OrdersByRating()
    {
        Assert.Equal(["bravo", "Charlie", "Alpha"], Names(Sorted(ItemSortBy.CommunityRating, SortOrder.Descending)));
    }

    [Fact]
    public void GetPeople_FavoritesFirst_ThenByName()
    {
        var query = Sorted(ItemSortBy.IsFavoriteOrLiked, SortOrder.Descending);
        query.User = _user;

        Assert.Equal(["Charlie", "Alpha", "bravo"], Names(query));
    }

    [Fact]
    public void GetPeople_FavoritesWithoutUser_FallsBackToNameInTheRequestedDirection()
    {
        Assert.Equal(["Charlie", "bravo", "Alpha"], Names(Sorted(ItemSortBy.IsFavoriteOrLiked, SortOrder.Descending)));
    }

    [Fact]
    public void GetPeople_KeyWithNoMeaningForAPerson_FallsBackToName()
    {
        Assert.Equal(["Alpha", "bravo", "Charlie"], Names(Sorted(ItemSortBy.Runtime, SortOrder.Ascending)));
    }

    [Fact]
    public void GetPeople_Random_StillReturnsEveryPersonOnce()
    {
        var names = Names(Sorted(ItemSortBy.Random, SortOrder.Ascending));

        Assert.Equal(["Alpha", "bravo", "Charlie"], names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetPeople_SortedPage_CountsEveryoneAndReturnsTheRequestedSlice()
    {
        var query = Sorted(ItemSortBy.DateCreated, SortOrder.Descending);
        query.StartIndex = 1;
        query.Limit = 1;

        var result = _repository.GetPeople(query);

        Assert.Equal(3, result.TotalRecordCount);
        Assert.Equal("Charlie", Assert.Single(result.Items).Name);
    }

    private static InternalPeopleQuery Sorted(ItemSortBy sortBy, SortOrder sortOrder) => new()
    {
        OrderBy = [(sortBy, sortOrder)]
    };

    private static BaseItemEntity Person(Guid id, string type, string name, int created, int born, float rating) => new()
    {
        Id = id,
        Type = type,
        Name = name,
        DateCreated = new DateTime(created, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PremiereDate = new DateTime(born, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CommunityRating = rating
    };

    private string[] Names(InternalPeopleQuery query) => _repository.GetPeople(query).Items.Select(p => p.Name).ToArray();
}
