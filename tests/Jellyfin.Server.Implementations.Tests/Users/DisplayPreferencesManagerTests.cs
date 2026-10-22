using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
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
    private static readonly Guid _otherUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _itemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly DisplayPreferencesManager _manager;

    public DisplayPreferencesManagerTests()
    {
        _manager = new DisplayPreferencesManager(CreateDbContextFactory());

        using var context = CreateDbContext();
        context.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
        context.Users.Add(new User("other-user", "auth-provider", "reset-provider") { Id = _otherUserId });
        context.SaveChanges();
    }

    [Fact]
    public void UpdateDisplayPreferences_NewSections_StoresTheSections()
    {
        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        preferences.HomeSections.Add(new HomeSection { Order = 0, Type = HomeSectionType.Resume });
        preferences.HomeSections.Add(new HomeSection { Order = 1, Type = HomeSectionType.NextUp });

        _manager.UpdateDisplayPreferences(preferences);

        using var context = CreateDbContext();
        Assert.Equal(
            [(0, HomeSectionType.Resume), (1, HomeSectionType.NextUp)],
            context.Set<HomeSection>()
                .OrderBy(section => section.Order)
                .AsEnumerable()
                .Select(section => (section.Order, section.Type)));
    }

    [Fact]
    public void UpdateDisplayPreferences_ReplacesSections_RemovesObsoleteRows()
    {
        SaveSections(_userId, (0, HomeSectionType.Resume), (1, HomeSectionType.NextUp), (2, HomeSectionType.LatestMedia));

        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        preferences.HomeSections.Clear();
        preferences.HomeSections.Add(new HomeSection { Order = 0, Type = HomeSectionType.LatestMedia });
        preferences.HomeSections.Add(new HomeSection { Order = 1, Type = HomeSectionType.Resume });
        _manager.UpdateDisplayPreferences(preferences);

        using var context = CreateDbContext();
        Assert.Equal(
            [(0, HomeSectionType.LatestMedia), (1, HomeSectionType.Resume)],
            context.Set<HomeSection>()
                .OrderBy(section => section.Order)
                .AsEnumerable()
                .Select(section => (section.Order, section.Type)));
    }

    [Fact]
    public void UpdateDisplayPreferences_RepeatedSaves_DoesNotDuplicateRows()
    {
        SaveSections(_userId, (0, HomeSectionType.Resume), (1, HomeSectionType.NextUp));
        SaveSections(_userId, (0, HomeSectionType.NextUp), (1, HomeSectionType.Resume));
        SaveSections(_userId, (0, HomeSectionType.Resume), (1, HomeSectionType.NextUp));

        using var context = CreateDbContext();
        Assert.Equal(2, context.Set<HomeSection>().Count());
    }

    [Fact]
    public void GetDisplayPreferences_ReturnsTheLatestSavedOrder()
    {
        SaveSections(_userId, (0, HomeSectionType.Resume), (1, HomeSectionType.NextUp));
        SaveSections(_userId, (0, HomeSectionType.LatestMedia), (1, HomeSectionType.Resume));

        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);

        Assert.Equal(
            [HomeSectionType.LatestMedia, HomeSectionType.Resume],
            preferences.HomeSections.OrderBy(section => section.Order).Select(section => section.Type));
    }

    [Fact]
    public void UpdateDisplayPreferences_DoesNotModifyAnotherUsersSections()
    {
        SaveSections(_userId, (0, HomeSectionType.Resume));
        SaveSections(_otherUserId, (0, HomeSectionType.NextUp));

        SaveSections(_userId, (0, HomeSectionType.LatestMedia), (1, HomeSectionType.Resume));

        var otherPreferences = _manager.GetDisplayPreferences(_otherUserId, _itemId, Client);
        Assert.Equal([HomeSectionType.NextUp], otherPreferences.HomeSections.Select(section => section.Type));
    }

    [Fact]
    public void UpdateDisplayPreferences_SectionFromAnotherUser_RejectsAndPreservesBothUsers()
    {
        SaveSections(_userId, (0, HomeSectionType.Resume));
        SaveSections(_otherUserId, (0, HomeSectionType.NextUp));

        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        var otherPreferences = _manager.GetDisplayPreferences(_otherUserId, _itemId, Client);
        preferences.HomeSections.Clear();
        preferences.HomeSections.Add(otherPreferences.HomeSections.Single());

        Assert.Throws<InvalidOperationException>(() => _manager.UpdateDisplayPreferences(preferences));

        var storedPreferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        var storedOtherPreferences = _manager.GetDisplayPreferences(_otherUserId, _itemId, Client);
        Assert.Equal([HomeSectionType.Resume], storedPreferences.HomeSections.Select(section => section.Type));
        Assert.Equal([HomeSectionType.NextUp], storedOtherPreferences.HomeSections.Select(section => section.Type));
    }

    [Fact]
    public void UpdateDisplayPreferences_ExistingPreferencesWithoutSections_StillWorks()
    {
        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        _manager.UpdateDisplayPreferences(preferences);

        var stored = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        Assert.Empty(stored.HomeSections);
    }

    [Fact]
    public void UpdateDisplayPreferences_UnchangedSections_PreservesExistingRows()
    {
        SaveSections(_userId, (0, HomeSectionType.Resume), (1, HomeSectionType.NextUp));

        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        preferences.ShowSidebar = !preferences.ShowSidebar;
        _manager.UpdateDisplayPreferences(preferences);

        var stored = _manager.GetDisplayPreferences(_userId, _itemId, Client);
        Assert.Equal(
            [(0, HomeSectionType.Resume), (1, HomeSectionType.NextUp)],
            stored.HomeSections
                .OrderBy(section => section.Order)
                .Select(section => (section.Order, section.Type)));
    }

    private void SaveSections(Guid userId, params (int Order, HomeSectionType Type)[] sections)
    {
        var preferences = _manager.GetDisplayPreferences(userId, _itemId, Client);
        preferences.HomeSections.Clear();
        foreach (var (order, type) in sections)
        {
            preferences.HomeSections.Add(new HomeSection { Order = order, Type = type });
        }

        _manager.UpdateDisplayPreferences(preferences);
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
