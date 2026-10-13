using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Devices;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Session;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// The device id comes from the client and can be longer than its column: the web client builds it from its user
/// agent. The manager shortens it wherever it stores or looks one up, so that the device is saved and found again.
/// </summary>
public sealed class DeviceManagerKeyLengthTests : JellyfinDbTestFixture
{
    private static readonly string _longId = new('d', KeyNormalizer.DeviceIdMaxLength + 84);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DeviceManager _manager;

    public DeviceManagerKeyLengthTests()
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = _userId });
            context.SaveChanges();
        }

        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUserById(_userId)).Returns(new User("user", "auth", "reset") { Id = _userId });
        _manager = new DeviceManager(CreateDbContextFactory(), userManager.Object);
    }

    [Fact]
    public async Task CreateDevice_DeviceIdOverTheBound_StoresTheShortenedId()
    {
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", _longId));

        using var context = CreateDbContext();
        Assert.Equal(KeyNormalizer.NormalizeDeviceId(_longId), Assert.Single(context.Devices).DeviceId);
    }

    [Fact]
    public async Task GetDevices_DeviceIdOverTheBound_FindsTheDeviceCreatedWithIt()
    {
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", _longId));

        var device = Assert.Single(_manager.GetDevices(new DeviceQuery { DeviceId = _longId, UserId = _userId }).Items);

        Assert.Equal(KeyNormalizer.NormalizeDeviceId(_longId), device.DeviceId);
    }

    [Fact]
    public async Task GetDevices_TwoLongIdsWithTheSamePrefix_AreTwoDevices()
    {
        var common = new string('c', KeyNormalizer.DeviceIdMaxLength);
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", common + "one"));
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", common + "two"));

        var device = Assert.Single(_manager.GetDevices(new DeviceQuery { DeviceId = common + "one", UserId = _userId }).Items);

        Assert.Equal(KeyNormalizer.NormalizeDeviceId(common + "one"), device.DeviceId);
        Assert.Equal(2, _manager.GetDevices(new DeviceQuery { UserId = _userId }).Items.Count);
    }

    [Fact]
    public async Task GetDevice_DeviceIdOverTheBound_FindsTheDeviceAndItsOptions()
    {
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", _longId));
        await _manager.UpdateDeviceOptions(_longId, "custom");

        var device = _manager.GetDevice(_longId);

        Assert.Equal(KeyNormalizer.NormalizeDeviceId(_longId), device?.Id);
        Assert.Equal("custom", device?.CustomName);
    }

    [Fact]
    public async Task UpdateDeviceOptions_DeviceIdOverTheBoundUpdatedTwice_UpdatesTheStoredOptions()
    {
        await _manager.UpdateDeviceOptions(_longId, "first");
        await _manager.UpdateDeviceOptions(_longId, "second");

        using var context = CreateDbContext();
        var options = Assert.Single(context.DeviceOptions);
        Assert.Equal(KeyNormalizer.NormalizeDeviceId(_longId), options.DeviceId);
        Assert.Equal("second", options.CustomName);
    }

    [Fact]
    public async Task GetDeviceOptions_DeviceIdOverTheBound_FindsTheOptions()
    {
        await _manager.UpdateDeviceOptions(_longId, "name");

        Assert.Equal("name", _manager.GetDeviceOptions(_longId)?.CustomName);
    }

    [Fact]
    public void GetCapabilities_DeviceIdOverTheBound_FindsTheCapabilitiesSavedUnderIt()
    {
        var capabilities = new ClientCapabilities();
        _manager.SaveCapabilities(_longId, capabilities);

        Assert.Same(capabilities, _manager.GetCapabilities(_longId));
    }

    [Fact]
    public void CanAccessDevice_DeviceIdOverTheBound_MatchesTheEnabledDeviceStoredShortened()
    {
        var user = new User("other", "auth", "reset");
        user.SetPermission(PermissionKind.EnableAllDevices, false);
        user.SetPreference(PreferenceKind.EnabledDevices, [KeyNormalizer.NormalizeDeviceId(_longId)]);
        _manager.SaveCapabilities(_longId, new ClientCapabilities { SupportsPersistentIdentifier = true });

        Assert.True(_manager.CanAccessDevice(user, _longId));
        Assert.False(_manager.CanAccessDevice(user, _longId + "other"));
    }
}
