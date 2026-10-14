using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using SortOrder = Jellyfin.Database.Implementations.Enums.SortOrder;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// An item's played date is the newest of its own and its alternate versions' dates, and an item with
/// neither sorts lowest. With a user in the query only that user's progress counts.
/// </summary>
public sealed class BaseItemRepositoryDatePlayedOrderTests : JellyfinDbTestFixture
{
    private const string NeverPlayed = "Never played";
    private const string OwnOnly = "Own only";
    private const string AlternateOnly = "Alternate only";
    private const string OwnNewer = "Own newer";
    private const string AlternateNewer = "Alternate newer";

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly User _user = new("test", "auth", "reset");
    private readonly User _otherUser = new("other", "auth", "reset");

    public BaseItemRepositoryDatePlayedOrderTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetItemList_DatePlayedAscending_UsesNewestOfOwnAndAlternateVersionDates()
    {
        Seed(withOtherUserProgress: false);

        Assert.Equal(
            [NeverPlayed, OwnOnly, AlternateOnly, OwnNewer, AlternateNewer],
            Names(_user, SortOrder.Ascending));
    }

    [Fact]
    public void GetItemList_DatePlayedDescending_ReversesTheAscendingOrder()
    {
        Seed(withOtherUserProgress: false);

        Assert.Equal(
            [AlternateNewer, OwnNewer, AlternateOnly, OwnOnly, NeverPlayed],
            Names(_user, SortOrder.Descending));
    }

    [Fact]
    public void GetItemList_DatePlayedWithUser_IgnoresOtherUsersProgress()
    {
        Seed(withOtherUserProgress: true);

        Assert.Equal(
            [NeverPlayed, OwnOnly, AlternateOnly, OwnNewer, AlternateNewer],
            Names(_user, SortOrder.Ascending));
    }

    [Fact]
    public void GetItemList_DatePlayedWithoutUser_UsesEveryUsersProgress()
    {
        Seed(withOtherUserProgress: true);

        Assert.Equal(
            [AlternateOnly, OwnNewer, AlternateNewer, OwnOnly, NeverPlayed],
            Names(null, SortOrder.Ascending));
    }

    private string[] Names(User? user, SortOrder sortOrder)
    {
        return _repository
            .GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                OrderBy = [(ItemSortBy.DatePlayed, sortOrder)]
            })
            .Select(i => i.Name)
            .ToArray();
    }

    private void Seed(bool withOtherUserProgress)
    {
        using var context = CreateDbContext();
        context.Users.AddRange(_user, _otherUser);

        var neverPlayed = AddMovie(context, NeverPlayed, primaryVersionId: null);
        var ownOnly = AddMovie(context, OwnOnly, primaryVersionId: null);
        var alternateOnly = AddMovie(context, AlternateOnly, primaryVersionId: null);
        var ownNewer = AddMovie(context, OwnNewer, primaryVersionId: null);
        var alternateNewer = AddMovie(context, AlternateNewer, primaryVersionId: null);

        var alternateOfAlternateOnly = AddMovie(context, "Alternate of alternate only", alternateOnly);
        var alternateOfOwnNewer = AddMovie(context, "Alternate of own newer", ownNewer);
        var alternateOfAlternateNewer = AddMovie(context, "Alternate of alternate newer", alternateNewer);
        var alternateOfOwnOnly = AddMovie(context, "Alternate of own only", ownOnly);

        AddProgress(context, _user, ownOnly, 2001);
        AddProgress(context, _user, alternateOfAlternateOnly, 2002);
        AddProgress(context, _user, ownNewer, 2006);
        AddProgress(context, _user, alternateOfOwnNewer, 2004);
        AddProgress(context, _user, alternateNewer, 2003);
        AddProgress(context, _user, alternateOfAlternateNewer, 2007);

        if (withOtherUserProgress)
        {
            AddProgress(context, _otherUser, neverPlayed, 2011);
            AddProgress(context, _otherUser, alternateOfOwnOnly, 2010);
        }

        context.SaveChanges();
    }

    private Guid AddMovie(JellyfinDbContext context, string name, Guid? primaryVersionId)
    {
        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = name,
            SortName = name,
            PresentationUniqueKey = id.ToString("N"),
            PrimaryVersionId = primaryVersionId,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        });

        return id;
    }

    private static void AddProgress(JellyfinDbContext context, User user, Guid itemId, int year)
    {
        context.UserData.Add(new UserData
        {
            ItemId = itemId,
            UserId = user.Id,
            CustomDataKey = itemId.ToString("N"),
            LastPlayedDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Item = null!,
            User = null!
        });
    }
}
