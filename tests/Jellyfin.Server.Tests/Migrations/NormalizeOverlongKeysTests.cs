using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.Devices;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The rows the migration rewrites were stored before the length of the key columns was limited. No provider limits
/// the length of the column itself, so the tests seed such a row around the model. The columns of an indexed string
/// have their maximum length on MySQL and MariaDB, where no such row can exist and the tests are skipped.
/// </summary>
public sealed class NormalizeOverlongKeysTests : DatabaseTestFixture
{
    private static readonly DateTime _older = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _newer = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Guid _itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _userId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly CapturingLogger<NormalizeOverlongKeys> _log = new();

    [Fact]
    public async Task Perform_UserDataKeyOverTheBound_IsRewrittenToTheNormalizedKey()
    {
        var overlong = new string('k', KeyNormalizer.UserDataKeyMaxLength + 44);
        var atBound = new string('b', KeyNormalizer.UserDataKeyMaxLength);
        await AddUserDataAsync(overlong, _older, playCount: 3);
        await AddUserDataAsync(atBound, _older, playCount: 1);
        await AddUserDataAsync("tt0001", null, playCount: 2);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var rows = ctx.UserData.AsNoTracking().ToList();
        Assert.Equal(
            new[] { KeyNormalizer.NormalizeUserDataKey(overlong), atBound, "tt0001" }.Order(StringComparer.Ordinal),
            rows.Select(e => e.CustomDataKey).Order(StringComparer.Ordinal));
        Assert.Equal(3, rows.Single(e => e.CustomDataKey == KeyNormalizer.NormalizeUserDataKey(overlong)).PlayCount);
        Assert.All(rows, e => Assert.True(e.CustomDataKey.Length <= KeyNormalizer.UserDataKeyMaxLength));
        Assert.Contains(_log.Messages, m => m.Contains("1 user data keys", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Perform_UserDataKeysSharingTheirFirstCharacters_StayApart()
    {
        var common = new string('c', KeyNormalizer.UserDataKeyMaxLength);
        await AddUserDataAsync(common + "one", _older, playCount: 1);
        await AddUserDataAsync(common + "two", _older, playCount: 2);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        Assert.Equal(2, ctx.UserData.AsNoTracking().Select(e => e.CustomDataKey).Distinct().Count());
    }

    [Fact]
    public async Task Perform_OverlongKeyCollidesWithTheNormalizedKey_KeepsTheMoreRecentlyPlayedRow()
    {
        var overlong = new string('k', KeyNormalizer.UserDataKeyMaxLength + 10);
        var normalized = KeyNormalizer.NormalizeUserDataKey(overlong);
        await AddUserDataAsync(normalized, _older, playCount: 1);
        await AddUserDataAsync(overlong, _newer, playCount: 7);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal(normalized, row.CustomDataKey);
        Assert.Equal(7, row.PlayCount);
        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Information && e.Message.Contains(_userId.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(_log.Messages, m => m.Contains(overlong, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Perform_NormalizedKeyIsMoreRecentlyPlayed_KeepsTheExistingRow()
    {
        var overlong = new string('k', KeyNormalizer.UserDataKeyMaxLength + 10);
        var normalized = KeyNormalizer.NormalizeUserDataKey(overlong);
        await AddUserDataAsync(normalized, _newer, playCount: 2);
        await AddUserDataAsync(overlong, _older, playCount: 9);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal(normalized, row.CustomDataKey);
        Assert.Equal(2, row.PlayCount);
    }

    [Fact]
    public async Task Perform_CollidingRowsPlayedAtTheSameTime_KeepsTheExistingRow()
    {
        var overlong = new string('k', KeyNormalizer.UserDataKeyMaxLength + 10);
        var normalized = KeyNormalizer.NormalizeUserDataKey(overlong);
        await AddUserDataAsync(normalized, _older, playCount: 2);
        await AddUserDataAsync(overlong, _older, playCount: 9);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal(2, row.PlayCount);
    }

    [Fact]
    public async Task Perform_DeviceIdsOverTheBound_AreRewrittenToTheNormalizedId()
    {
        var overlong = new string('d', KeyNormalizer.DeviceIdMaxLength + 50);
        var atBound = new string('e', KeyNormalizer.DeviceIdMaxLength);
        await AddDeviceAsync(overlong);
        await AddDeviceAsync(overlong);
        await AddDeviceAsync(atBound);
        await AddDeviceAsync("short");

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var ids = ctx.Devices.AsNoTracking().Select(e => e.DeviceId).ToList();
        Assert.Equal(2, ids.Count(e => e == KeyNormalizer.NormalizeDeviceId(overlong)));
        Assert.Contains(atBound, ids);
        Assert.Contains("short", ids);
        Assert.Equal(4, ids.Count);
    }

    [Fact]
    public async Task Perform_DeviceOptionsOverTheBound_AreRewrittenAndKeepTheirName()
    {
        var overlong = new string('d', KeyNormalizer.DeviceIdMaxLength + 50);
        await AddDeviceOptionsAsync(overlong, "Living room");
        await AddDeviceOptionsAsync("short", "Phone");

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var options = ctx.DeviceOptions.AsNoTracking().ToList();
        Assert.Equal("Living room", options.Single(e => e.DeviceId == KeyNormalizer.NormalizeDeviceId(overlong)).CustomName);
        Assert.Equal("Phone", options.Single(e => e.DeviceId == "short").CustomName);
    }

    [Fact]
    public async Task Perform_DeviceOptionsCollideWithTheNormalizedId_KeepTheExistingRow()
    {
        var overlong = new string('d', KeyNormalizer.DeviceIdMaxLength + 50);
        var normalized = KeyNormalizer.NormalizeDeviceId(overlong);
        await AddDeviceOptionsAsync(normalized, "Kept");
        await AddDeviceOptionsAsync(overlong, "Dropped");

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.DeviceOptions.AsNoTracking().ToList());
        Assert.Equal("Kept", row.CustomName);
        Assert.Equal(normalized, row.DeviceId);
    }

    [Fact]
    public async Task Perform_EnabledDevicesWithAnEntryOverTheBound_AreRewrittenAndKeepTheDeviceAccessible()
    {
        var overlong = new string('d', KeyNormalizer.DeviceIdMaxLength + 50);
        await EnsureUserAndItemAsync();
        using (var ctx = CreateDbContext())
        {
            var stored = ctx.Users.Include(e => e.Preferences).Single(e => e.Id.Equals(_userId));
            stored.SetPreference(PreferenceKind.EnabledDevices, [overlong, "short", overlong]);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var verify = CreateDbContext();
        var user = verify.Users.AsNoTracking().Include(e => e.Preferences).Single(e => e.Id.Equals(_userId));
        Assert.Equal([KeyNormalizer.NormalizeDeviceId(overlong), "short"], user.GetPreference(PreferenceKind.EnabledDevices));
        user.SetPermission(PermissionKind.EnableAllDevices, false);
        var deviceManager = new DeviceManager(CreateDbContextFactory(), new Mock<IUserManager>().Object);
        deviceManager.SaveCapabilities(overlong, new ClientCapabilities { SupportsPersistentIdentifier = true });
        Assert.True(deviceManager.CanAccessDevice(user, overlong));
        Assert.Contains("1 users", _log.Messages.Single(m => m.Contains("Shortening", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Perform_SecondRun_FindsNothingToDo()
    {
        await AddUserDataAsync(new string('k', KeyNormalizer.UserDataKeyMaxLength + 1), _older, playCount: 1);
        await AddDeviceAsync(new string('d', KeyNormalizer.DeviceIdMaxLength + 1));
        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);
        _log.Entries.Clear();

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_log.Entries);
    }

    [Fact]
    public async Task Perform_ValuesAtTheBound_AreLeftAloneWithoutLogging()
    {
        var key = new string('k', KeyNormalizer.UserDataKeyMaxLength);
        var deviceId = new string('d', KeyNormalizer.DeviceIdMaxLength);
        await AddUserDataAsync(key, _older, playCount: 1, overlong: false);
        await AddDeviceAsync(deviceId, overlong: false);
        await AddDeviceOptionsAsync(deviceId, "Name", overlong: false);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        Assert.Equal(key, Assert.Single(ctx.UserData.AsNoTracking().ToList()).CustomDataKey);
        Assert.Equal(deviceId, Assert.Single(ctx.Devices.AsNoTracking().ToList()).DeviceId);
        Assert.Equal(deviceId, Assert.Single(ctx.DeviceOptions.AsNoTracking().ToList()).DeviceId);
        Assert.Empty(_log.Entries);
    }

    private async Task EnsureUserAndItemAsync()
    {
        using var ctx = CreateDbContext();
        if (!ctx.Users.Any(e => e.Id.Equals(_userId)))
        {
            ctx.Users.Add(new User("test", "auth", "reset") { Id = _userId });
        }

        if (!ctx.BaseItems.Any(e => e.Id.Equals(_itemId)))
        {
            ctx.BaseItems.Add(new BaseItemEntity { Id = _itemId, Type = typeof(Season).FullName!, Name = "Season 1" });
        }

        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // The overlong values are written around the model, the way they were stored before the length was limited.
    private async Task AddUserDataAsync(string key, DateTime? lastPlayed, int playCount, bool overlong = true)
    {
        SkipWhenOverlongCannotBeStored(overlong);
        await EnsureUserAndItemAsync();
        using var ctx = CreateDbContext();
        ctx.UserData.Add(new UserData
        {
            ItemId = _itemId,
            Item = null,
            UserId = _userId,
            User = null,
            CustomDataKey = overlong ? "placeholder" : key,
            LastPlayedDate = lastPlayed,
            PlayCount = playCount
        });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (overlong)
        {
            await ctx.UserData
                .Where(e => e.ItemId.Equals(_itemId) && e.UserId.Equals(_userId) && e.CustomDataKey == "placeholder")
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.CustomDataKey, key), TestContext.Current.CancellationToken);
        }
    }

    private async Task AddDeviceAsync(string deviceId, bool overlong = true)
    {
        SkipWhenOverlongCannotBeStored(overlong);
        await EnsureUserAndItemAsync();
        using var ctx = CreateDbContext();
        var device = new Device(_userId, "App", "1.0", "Name", overlong ? "placeholder" : deviceId);
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (overlong)
        {
            var id = device.Id;
            await ctx.Devices
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.DeviceId, deviceId), TestContext.Current.CancellationToken);
        }
    }

    private async Task AddDeviceOptionsAsync(string deviceId, string name, bool overlong = true)
    {
        SkipWhenOverlongCannotBeStored(overlong);
        using var ctx = CreateDbContext();
        var options = new DeviceOptions(overlong ? "placeholder-" + name : deviceId) { CustomName = name };
        ctx.DeviceOptions.Add(options);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (overlong)
        {
            var id = options.Id;
            await ctx.DeviceOptions
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.DeviceId, deviceId), TestContext.Current.CancellationToken);
        }
    }

    private static void SkipWhenOverlongCannotBeStored(bool overlong)
    {
        Assert.SkipWhen(
            overlong && TestDatabaseEnvironment.Provider is TestDbProvider.MySql or TestDbProvider.MariaDb,
            "The column of an indexed string has its maximum length on this engine, so no row can be stored over it.");
    }

    private NormalizeOverlongKeys CreateMigration()
    {
        return new NormalizeOverlongKeys(
            new StartupLogger<NormalizeOverlongKeys>(_log),
            CreateDbContextFactory());
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IEnumerable<string> Messages => Entries.Select(e => e.Message);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
