using System;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Users;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// Two first-ever requests for the same preferences can both find nothing and both insert; the unique index lets one
/// through. The other has to return the stored preferences instead of failing.
/// </summary>
public sealed class DisplayPreferencesManagerConcurrentCreateTests : SqliteDbTestFixture
{
    private const string Client = "client";

    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _itemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly CompetingInsert _competingInsert;
    private readonly DisplayPreferencesManager _manager;

    public DisplayPreferencesManagerConcurrentCreateTests()
        : this(new CompetingInsert())
    {
    }

    private DisplayPreferencesManagerConcurrentCreateTests(CompetingInsert competingInsert)
        : base(competingInsert)
    {
        _competingInsert = competingInsert;
        _manager = new DisplayPreferencesManager(CreateDbContextFactory());

        using var context = CreateDbContext();
        context.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
        context.SaveChanges();
    }

    [Fact]
    public void GetDisplayPreferences_AnotherRequestStoresThemFirst_ReturnsTheStoredPreferences()
    {
        var storedId = 0;
        _competingInsert.Before = () =>
        {
            using var context = CreateDbContext();
            var stored = new DisplayPreferences(_userId, _itemId, Client);
            context.DisplayPreferences.Add(stored);
            context.SaveChanges();
            storedId = stored.Id;
        };

        var preferences = _manager.GetDisplayPreferences(_userId, _itemId, Client);

        Assert.NotEqual(0, storedId);
        Assert.Equal(storedId, preferences.Id);
        using var check = CreateDbContext();
        Assert.Single(check.DisplayPreferences);
    }

    /// <summary>
    /// Stores the other request's row just before the next save, after the manager has looked for the preferences.
    /// </summary>
    private sealed class CompetingInsert : SaveChangesInterceptor
    {
        public Action? Before { get; set; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            // Disarmed first, because the competing save goes through this interceptor as well.
            var before = Before;
            Before = null;
            before?.Invoke();
            return result;
        }
    }
}
