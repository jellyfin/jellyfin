using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Server.Implementations.Tests.Item;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// A NUL character in a string property is removed when the entity is saved, so that it is never stored:
/// some engines reject it in text. The text after the NUL is kept. The properties that are part of a key or of a
/// unique index are not changed: code that compares them with stored rows has to clean them first, and a value
/// cleaned on save would no longer be the string it compared.
/// </summary>
public sealed class SaveChangesNulTests : SqliteDbTestFixture
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_StripsNulFromStrings(bool useAsync)
    {
        var itemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateItem(itemId, "Mid\0dle", "Overview\0 and more\0"));
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            context.ActivityLogs.Add(new ActivityLog("Logged\0 in\0", "Test", userId) { Overview = "Over\0view\0" });
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var item = context.BaseItems.AsNoTracking().Single(e => e.Id.Equals(itemId));
            Assert.Equal("Middle", item.Name);
            Assert.Equal("Overview and more", item.Overview);

            var activity = context.ActivityLogs.AsNoTracking().Single();
            Assert.Equal("Logged in", activity.Name);
            Assert.Equal("Overview", activity.Overview);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_ModifiedEntity_StripsNulFromStrings(bool useAsync)
    {
        var itemId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateItem(itemId, "Name", "Overview"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var item = context.BaseItems.Single(e => e.Id.Equals(itemId));
            item.Name = "Chan\0ged";
            item.Overview = "New\0 overview\0";
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var item = context.BaseItems.AsNoTracking().Single(e => e.Id.Equals(itemId));
            Assert.Equal("Changed", item.Name);
            Assert.Equal("New overview", item.Overview);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_ModifiedEntity_StripsLegacyNulFromAColumnItDidNotChange(bool useAsync)
    {
        var itemId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateItem(itemId, "Name", "Overview"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Written around the save hook, as rows saved by an earlier version were.
            await context.BaseItems
                .Where(e => e.Id.Equals(itemId))
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.Overview, "Leg\0acy"), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var item = context.BaseItems.Single(e => e.Id.Equals(itemId));
            Assert.Equal("Leg\0acy", item.Overview);
            item.Name = "Changed";
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var item = context.BaseItems.AsNoTracking().Single(e => e.Id.Equals(itemId));
            Assert.Equal("Changed", item.Name);
            Assert.Equal("Legacy", item.Overview);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_ModifiedEntityWithConcurrencyToken_BumpsTheTokenAndStripsNul(bool useAsync)
    {
        var userId = Guid.NewGuid();
        int logId;
        uint rowVersion;

        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            var log = new ActivityLog("Logged in", "Test", userId) { Overview = "Overview" };
            context.ActivityLogs.Add(log);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            logId = log.Id;
        }

        using (var context = CreateDbContext())
        {
            var log = context.ActivityLogs.Single(e => e.Id == logId);
            rowVersion = log.RowVersion;
            log.Overview = "Chan\0ged";
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var log = context.ActivityLogs.AsNoTracking().Single(e => e.Id == logId);
            Assert.Equal(rowVersion + 1, log.RowVersion);
            Assert.Equal("Changed", log.Overview);
        }
    }

    [Fact]
    public void SaveChanges_ModifiedEntityWithNulInKey_SavesTheOtherChanges()
    {
        var itemId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateItem(itemId, "Name", "Overview"));
            context.SaveChanges();

            context.Database.ExecuteSql(
                $"INSERT INTO BaseItemProviders (ItemId, ProviderId, ProviderValue) VALUES ({itemId}, 'imdb' || char(0), 'value')");
        }

        using (var context = CreateDbContext())
        {
            var provider = context.BaseItemProviders.Single();
            provider.ProviderValue = "changed\0";
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            var provider = context.BaseItemProviders.AsNoTracking().Single();
            Assert.Equal("imdb\0", provider.ProviderId);
            Assert.Equal("changed", provider.ProviderValue);
        }
    }

    [Theory]
    [InlineData("ItemValues")]
    [InlineData("ProviderIds")]
    [InlineData("UserData")]
    [InlineData("DisplayPreferences")]
    [InlineData("CustomItemDisplayPreferences")]
    [InlineData("DeviceOptions")]
    public void SaveChanges_UniqueValuesDifferingOnlyByNul_AreBothSaved(string shape)
    {
        var itemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateItem(itemId, "Name", "Overview"));
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            AddRows(context, shape, itemId, userId, "value", "value\0");
            context.SaveChanges();
        }

        using var readContext = CreateDbContext();
        Assert.Equal(["value", "value\0"], StoredValues(readContext, shape).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SaveChanges_ModifiedEntityWithNulInUniqueColumn_SavesTheOtherChangesAndKeepsTheColumn()
    {
        using (var context = CreateDbContext())
        {
            context.ItemValues.Add(new ItemValue { ItemValueId = Guid.NewGuid(), Type = ItemValueType.Genre, Value = "Rock", CleanValue = "rock" });
            context.SaveChanges();

            // Written around the save hook, as rows saved by an earlier version were.
            context.Database.ExecuteSql(
                $"INSERT INTO ItemValues (ItemValueId, Type, Value, CleanValue) VALUES ({Guid.NewGuid()}, {(int)ItemValueType.Genre}, 'Ro' || char(0) || 'ck', 'ro ck')");
        }

        using (var context = CreateDbContext())
        {
            foreach (var value in context.ItemValues.ToList())
            {
                value.CleanValue = value.Value.Contains('\0', StringComparison.Ordinal) ? "ro\0ck" : "rock!";
            }

            context.SaveChanges();
        }

        using var readContext = CreateDbContext();
        var values = readContext.ItemValues.AsNoTracking().OrderBy(e => e.CleanValue).ToList();
        Assert.Equal(["Ro\0ck", "Rock"], values.Select(e => e.Value));
        Assert.Equal(["rock", "rock!"], values.Select(e => e.CleanValue));
    }

    [Fact]
    public void SaveChanges_NulInUniqueColumn_IsKeptWhileTheOtherColumnsOfTheEntityAreCleaned()
    {
        using (var context = CreateDbContext())
        {
            context.ItemValues.Add(new ItemValue { ItemValueId = Guid.NewGuid(), Type = ItemValueType.Genre, Value = "Ro\0ck", CleanValue = "ro\0ck" });
            context.SaveChanges();
        }

        using var readContext = CreateDbContext();
        var value = readContext.ItemValues.AsNoTracking().Single();
        Assert.Equal("Ro\0ck", value.Value);
        Assert.Equal("rock", value.CleanValue);
    }

    private static void AddRows(JellyfinDbContext context, string shape, Guid itemId, Guid userId, params string[] values)
    {
        foreach (var value in values)
        {
            switch (shape)
            {
                case "ItemValues":
                    context.ItemValues.Add(new ItemValue { ItemValueId = Guid.NewGuid(), Type = ItemValueType.Genre, Value = value, CleanValue = "value" });
                    break;
                case "ProviderIds":
                    context.BaseItemProviders.Add(new BaseItemProvider { ItemId = itemId, Item = null!, ProviderId = value, ProviderValue = "id" });
                    break;
                case "UserData":
                    context.UserData.Add(new UserData { ItemId = itemId, Item = null, UserId = userId, User = null, CustomDataKey = value });
                    break;
                case "DisplayPreferences":
                    context.DisplayPreferences.Add(new DisplayPreferences(userId, Guid.Empty, value));
                    break;
                case "CustomItemDisplayPreferences":
                    context.CustomItemDisplayPreferences.Add(new CustomItemDisplayPreferences(userId, itemId, "client", value, "value"));
                    break;
                case "DeviceOptions":
                    context.DeviceOptions.Add(new DeviceOptions(value));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }
    }

    private static string[] StoredValues(JellyfinDbContext context, string shape) => shape switch
    {
        "ItemValues" => context.ItemValues.AsNoTracking().Select(e => e.Value).ToArray(),
        "ProviderIds" => context.BaseItemProviders.AsNoTracking().Select(e => e.ProviderId).ToArray(),
        "UserData" => context.UserData.AsNoTracking().Select(e => e.CustomDataKey).ToArray(),
        "DisplayPreferences" => context.DisplayPreferences.AsNoTracking().Select(e => e.Client).ToArray(),
        "CustomItemDisplayPreferences" => context.CustomItemDisplayPreferences.AsNoTracking().Select(e => e.Key).ToArray(),
        "DeviceOptions" => context.DeviceOptions.AsNoTracking().Select(e => e.DeviceId).ToArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
    };

#pragma warning disable CA1849 // The synchronous overload is one of the two cases under test.
    private static Task SaveAsync(JellyfinDbContext context, bool useAsync)
    {
        return useAsync
            ? context.SaveChangesAsync(TestContext.Current.CancellationToken)
            : Task.FromResult(context.SaveChanges());
    }
#pragma warning restore CA1849

    private static BaseItemEntity CreateItem(Guid id, string name, string overview) => new()
    {
        Id = id,
        PresentationUniqueKey = id.ToString("N"),
        Type = "MediaBrowser.Controller.Entities.Movies.Movie",
        Name = name,
        Overview = overview,
        MediaType = "Video",
        IsMovie = true,
        IsFolder = false,
        IsVirtualItem = false
    };
}
