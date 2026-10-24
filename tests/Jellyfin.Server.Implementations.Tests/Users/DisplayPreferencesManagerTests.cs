using System;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// Item display preferences are looked up by the item they belong to, so they have to be stored under it as well:
/// a row stored under any other item is never found again, and every later request stores another one.
/// </summary>
public sealed class DisplayPreferencesManagerTests : SqliteDbTestFixture
{
    private const string Client = "client";

    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _itemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly DisplayPreferencesManager _manager;

    public DisplayPreferencesManagerTests()
    {
        _manager = new DisplayPreferencesManager(CreateDbContextFactory());

        using var context = CreateDbContext();
        context.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
        context.SaveChanges();
    }

    [Fact]
    public void GetItemDisplayPreferences_NewItem_StoresThePreferencesUnderThatItem()
    {
        var preferences = _manager.GetItemDisplayPreferences(_userId, _itemId, Client);

        Assert.Equal(_itemId, preferences.ItemId);

        using var context = CreateDbContext();
        Assert.Equal(_itemId, Assert.Single(context.ItemDisplayPreferences).ItemId);
    }

    [Fact]
    public void GetItemDisplayPreferences_SameItemTwice_ReturnsTheStoredPreferences()
    {
        var first = _manager.GetItemDisplayPreferences(_userId, _itemId, Client);
        var second = _manager.GetItemDisplayPreferences(_userId, _itemId, Client);

        Assert.Equal(first.Id, second.Id);

        using var context = CreateDbContext();
        Assert.Single(context.ItemDisplayPreferences);
    }
}
