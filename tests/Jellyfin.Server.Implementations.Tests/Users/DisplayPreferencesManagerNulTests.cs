using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// The client name comes from the request and the preferences are stored under a unique client name, so it is
/// cleaned of NUL characters before it is stored or looked up. Otherwise a client name that differs from the
/// stored one only by a NUL is added again and the stored preferences are never found.
/// </summary>
public sealed class DisplayPreferencesManagerNulTests : JellyfinDbTestFixture
{
    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _itemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly DisplayPreferencesManager _manager;

    public DisplayPreferencesManagerNulTests()
    {
        _manager = new DisplayPreferencesManager(CreateDbContextFactory());

        using var context = CreateDbContext();
        context.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
        context.SaveChanges();
    }

    [Fact]
    public void GetDisplayPreferences_ClientWithNulRequestedTwice_ReturnsTheStoredPreferences()
    {
        var first = _manager.GetDisplayPreferences(_userId, _itemId, "client\0");
        var second = _manager.GetDisplayPreferences(_userId, _itemId, "client\0");

        Assert.Equal(first.Id, second.Id);

        using var context = CreateDbContext();
        Assert.Equal("client", Assert.Single(context.DisplayPreferences).Client);
    }

    [Fact]
    public void GetItemDisplayPreferences_ClientWithNulRequestedTwice_ReturnsTheStoredPreferences()
    {
        var first = _manager.GetItemDisplayPreferences(_userId, _itemId, "client\0");
        var second = _manager.GetItemDisplayPreferences(_userId, _itemId, "client\0");

        Assert.Equal(first.Id, second.Id);

        using var context = CreateDbContext();
        Assert.Equal("client", Assert.Single(context.ItemDisplayPreferences).Client);
    }

    [Fact]
    public void ListItemDisplayPreferences_ClientWithNul_FindsThePreferencesStoredUnderTheCleanedClient()
    {
        var stored = _manager.GetItemDisplayPreferences(_userId, _itemId, "client");

        var listed = _manager.ListItemDisplayPreferences(_userId, "cli\0ent");

        Assert.Equal(stored.Id, Assert.Single(listed).Id);
    }

    [Fact]
    public void CustomItemDisplayPreferences_ClientWithNul_AreReadBackUnderTheCleanedClient()
    {
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, "client", new Dictionary<string, string?> { ["key"] = "value" });

        var listed = _manager.ListCustomItemDisplayPreferences(_userId, _itemId, "client\0");

        Assert.Equal("value", listed["key"]);
    }

    [Fact]
    public void SetCustomItemDisplayPreferences_KeysDifferingOnlyByNul_StoreOneKey()
    {
        _manager.SetCustomItemDisplayPreferences(
            _userId,
            _itemId,
            "client",
            new Dictionary<string, string?> { ["key"] = "first", ["key\0"] = "second" });

        using var context = CreateDbContext();
        Assert.Equal("key", Assert.Single(context.CustomItemDisplayPreferences).Key);
    }

    [Fact]
    public void SetCustomItemDisplayPreferences_ClientWithNulSetTwice_ReplacesTheStoredKeys()
    {
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, "client\0", new Dictionary<string, string?> { ["key"] = "first" });
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, "client\0", new Dictionary<string, string?> { ["key"] = "second" });

        using var context = CreateDbContext();
        var row = Assert.Single(context.CustomItemDisplayPreferences);
        Assert.Equal("client", row.Client);
        Assert.Equal("second", row.Value);
    }
}
