using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
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

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies that user-list filters and ordering translate and evaluate correctly on SQLite.
/// </summary>
public sealed class UserListQueryTranslationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly BaseItemRepository _repository;

    public UserListQueryTranslationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            new ItemTypeLookup(),
            configurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    [Fact]
    public void IsInWatchlistFalse_IncludesNonMembersWithAndWithoutUserData()
    {
        User user;
        BaseItemEntity member;
        BaseItemEntity nonMemberWithUserData;
        BaseItemEntity untouched;

        using (var context = CreateDbContext())
        {
            user = CreateUser("false-filter");
            var defaultList = CreateList(user.Id, "Watchlist", isDefault: true);
            member = CreateItem("A Member");
            nonMemberWithUserData = CreateItem("B Nonmember With User Data");
            untouched = CreateItem("C Untouched");

            context.Users.Add(user);
            context.UserLists.Add(defaultList);
            context.BaseItems.AddRange(member, nonMemberWithUserData, untouched);
            context.UserListItems.Add(CreateListItem(defaultList, member, new DateTime(2026, 1, 1)));
            context.UserData.Add(CreateUserData(user, nonMemberWithUserData, "nonmember-key"));
            context.SaveChanges();

            Assert.False(context.UserData.Any(
                data => data.ItemId.Equals(untouched.Id) && data.UserId.Equals(user.Id)));
            Assert.False(context.UserListItems.Any(entry => entry.ItemId.Equals(nonMemberWithUserData.Id)));
            Assert.False(context.UserListItems.Any(entry => entry.ItemId.Equals(untouched.Id)));
        }

        var query = CreateScopedQuery(user, member, nonMemberWithUserData, untouched);
        query.IsInWatchlist = false;

        var result = _repository.GetItemIdsList(query);

        Assert.Equal([nonMemberWithUserData.Id, untouched.Id], result);
    }

    [Fact]
    public void IsInWatchlistTrue_ReturnsExactlyDefaultListMembers()
    {
        User user;
        BaseItemEntity firstDefaultMember;
        BaseItemEntity secondDefaultMember;
        BaseItemEntity customListMember;
        BaseItemEntity nonMember;

        using (var context = CreateDbContext())
        {
            user = CreateUser("true-filter");
            var defaultList = CreateList(user.Id, "Watchlist", isDefault: true);
            var customList = CreateList(user.Id, "Custom", isDefault: false);
            firstDefaultMember = CreateItem("A Default Member");
            secondDefaultMember = CreateItem("B Default Member");
            customListMember = CreateItem("C Custom Member");
            nonMember = CreateItem("D Nonmember");

            context.Users.Add(user);
            context.UserLists.AddRange(defaultList, customList);
            context.BaseItems.AddRange(firstDefaultMember, secondDefaultMember, customListMember, nonMember);
            context.UserListItems.AddRange(
                CreateListItem(defaultList, firstDefaultMember, new DateTime(2026, 1, 1)),
                CreateListItem(defaultList, secondDefaultMember, new DateTime(2026, 1, 2)),
                CreateListItem(customList, customListMember, new DateTime(2026, 1, 3)));
            context.SaveChanges();
        }

        var query = CreateScopedQuery(user, firstDefaultMember, secondDefaultMember, customListMember, nonMember);
        query.IsInWatchlist = true;

        var result = _repository.GetItemIdsList(query);

        Assert.Equal([firstDefaultMember.Id, secondDefaultMember.Id], result);
    }

    [Fact]
    public void IsInWatchlistTrue_ItemWithMultipleUserDataRows_ReturnsItemOnce()
    {
        User user;
        BaseItemEntity member;
        BaseItemEntity nonMember;

        using (var context = CreateDbContext())
        {
            user = CreateUser("multiple-user-data");
            var defaultList = CreateList(user.Id, "Watchlist", isDefault: true);
            member = CreateItem("A Member");
            nonMember = CreateItem("B Nonmember");

            context.Users.Add(user);
            context.UserLists.Add(defaultList);
            context.BaseItems.AddRange(member, nonMember);
            context.UserListItems.Add(CreateListItem(defaultList, member, new DateTime(2026, 1, 1)));
            context.UserData.AddRange(
                CreateUserData(user, member, "first-key"),
                CreateUserData(user, member, "second-key"));
            context.SaveChanges();

            Assert.Equal(
                2,
                context.UserData.Count(
                    data => data.ItemId.Equals(member.Id) && data.UserId.Equals(user.Id)));
        }

        var query = CreateScopedQuery(user, member, nonMember);
        query.IsInWatchlist = true;

        var result = _repository.GetItemIdsList(query);

        Assert.Equal(member.Id, Assert.Single(result));
    }

    [Fact]
    public void DetachedEntry_IsExcludedFromWatchlistAndSelectedListFilters()
    {
        User user;
        UserList defaultList;
        BaseItemEntity attached;
        BaseItemEntity detached;

        using (var context = CreateDbContext())
        {
            user = CreateUser("detached-filter");
            defaultList = CreateList(user.Id, "Watchlist", isDefault: true);
            attached = CreateItem("A Attached");
            detached = CreateItem("B Detached");

            context.Users.Add(user);
            context.UserLists.Add(defaultList);
            context.BaseItems.AddRange(attached, detached);
            context.UserListItems.AddRange(
                CreateListItem(defaultList, attached, new DateTime(2026, 1, 1)),
                CreateDetachedListItem(defaultList, detached, new DateTime(2026, 1, 2)));
            context.SaveChanges();
        }

        var watchlistQuery = CreateScopedQuery(user, attached, detached);
        watchlistQuery.IsInWatchlist = true;

        Assert.Equal(attached.Id, Assert.Single(_repository.GetItemIdsList(watchlistQuery)));

        var selectedListQuery = CreateScopedQuery(user, attached, detached);
        selectedListQuery.UserListId = defaultList.Id;

        Assert.Equal(attached.Id, Assert.Single(_repository.GetItemIdsList(selectedListQuery)));
    }

    [Fact]
    public void UserListId_ReturnsOnlySelectedListMembers()
    {
        User user;
        UserList selectedList;
        BaseItemEntity selectedOnly;
        BaseItemEntity shared;
        BaseItemEntity otherOnly;

        using (var context = CreateDbContext())
        {
            user = CreateUser("list-id-filter");
            selectedList = CreateList(user.Id, "Selected", isDefault: false);
            var otherList = CreateList(user.Id, "Other", isDefault: false);
            selectedOnly = CreateItem("A Selected Only");
            shared = CreateItem("B Shared");
            otherOnly = CreateItem("C Other Only");

            context.Users.Add(user);
            context.UserLists.AddRange(selectedList, otherList);
            context.BaseItems.AddRange(selectedOnly, shared, otherOnly);
            context.UserListItems.AddRange(
                CreateListItem(selectedList, selectedOnly, new DateTime(2026, 1, 1)),
                CreateListItem(selectedList, shared, new DateTime(2026, 1, 2)),
                CreateListItem(otherList, shared, new DateTime(2026, 1, 3)),
                CreateListItem(otherList, otherOnly, new DateTime(2026, 1, 4)));
            context.SaveChanges();
        }

        var query = CreateScopedQuery(user, selectedOnly, shared, otherOnly);
        query.UserListId = selectedList.Id;

        var result = _repository.GetItemIdsList(query);

        Assert.Equal([selectedOnly.Id, shared.Id], result);
    }

    [Fact]
    public void DateAddedToList_OrdersBySelectedListEntryDate()
    {
        User user;
        UserList selectedList;
        BaseItemEntity olderInSelectedList;
        BaseItemEntity newerInSelectedList;

        using (var context = CreateDbContext())
        {
            user = CreateUser("date-added-sort");
            selectedList = CreateList(user.Id, "Selected", isDefault: false);
            var otherList = CreateList(user.Id, "Other", isDefault: false);
            olderInSelectedList = CreateItem("A Older In Selected");
            newerInSelectedList = CreateItem("B Newer In Selected");

            context.Users.Add(user);
            context.UserLists.AddRange(selectedList, otherList);
            context.BaseItems.AddRange(olderInSelectedList, newerInSelectedList);
            context.UserListItems.AddRange(
                CreateListItem(selectedList, olderInSelectedList, new DateTime(2026, 1, 1)),
                CreateListItem(selectedList, newerInSelectedList, new DateTime(2026, 1, 4)),
                CreateListItem(otherList, olderInSelectedList, new DateTime(2026, 1, 5)),
                CreateListItem(otherList, newerInSelectedList, new DateTime(2026, 1, 2)));
            context.SaveChanges();
        }

        var query = CreateScopedQuery(user, olderInSelectedList, newerInSelectedList);
        query.UserListId = selectedList.Id;
        query.OrderBy = [(ItemSortBy.DateAddedToList, SortOrder.Descending)];

        var result = _repository.GetItemIdsList(query);

        Assert.Equal([newerInSelectedList.Id, olderInSelectedList.Id], result);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static InternalItemsQuery CreateScopedQuery(User user, params BaseItemEntity[] items)
    {
        // EnsureCreated also seeds the repository placeholder row, so every assertion is
        // scoped through the identifiers created by the test.
        return new InternalItemsQuery(user)
        {
            GroupByPresentationUniqueKey = false,
            ItemIds = items.Select(item => item.Id).ToArray()
        };
    }

    private static User CreateUser(string name)
    {
        return new User(name, "authentication", "password-reset");
    }

    private static UserList CreateList(Guid userId, string name, bool isDefault)
    {
        return new UserList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Kind = isDefault ? UserListKind.Watchlist : UserListKind.Custom,
            IsDefault = isDefault,
            AutoRemoveWatched = isDefault,
            DateCreated = new DateTime(2026, 1, 1),
            DateModified = new DateTime(2026, 1, 1)
        };
    }

    private static BaseItemEntity CreateItem(string name)
    {
        var id = Guid.NewGuid();
        return new BaseItemEntity
        {
            Id = id,
            Type = "MediaBrowser.Controller.Entities.Movies.Movie",
            Name = name,
            SortName = name,
            PresentationUniqueKey = id.ToString("N"),
            MediaType = "Video",
            IsMovie = true
        };
    }

    private static UserListItem CreateListItem(UserList list, BaseItemEntity item, DateTime dateAdded)
    {
        return new UserListItem
        {
            UserListId = list.Id,
            UserList = list,
            CustomDataKey = item.Id.ToString(),
            ItemId = item.Id,
            Item = item,
            DateAdded = dateAdded
        };
    }

    private static UserListItem CreateDetachedListItem(UserList list, BaseItemEntity item, DateTime dateAdded)
    {
        return new UserListItem
        {
            UserListId = list.Id,
            UserList = list,
            CustomDataKey = item.Id.ToString(),
            ItemId = null,
            Item = null,
            DateAdded = dateAdded,
            RetentionDate = new DateTime(2026, 1, 3)
        };
    }

    private static UserData CreateUserData(User user, BaseItemEntity item, string customDataKey)
    {
        return new UserData
        {
            ItemId = item.Id,
            Item = item,
            UserId = user.Id,
            User = user,
            CustomDataKey = customDataKey
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
