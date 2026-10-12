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
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// The device id comes from the request and the options are stored under a unique device id, so it is cleaned
/// of NUL characters before it is stored or looked up. Otherwise a device id that differs from the stored one
/// only by a NUL is added again and the stored options are never found. The same goes for the devices: whoever
/// calls the manager, a device id with a NUL finds the device stored without it.
/// </summary>
public sealed class DeviceManagerNulTests : JellyfinDbTestFixture
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly User _user;
    private readonly DeviceManager _manager;

    public DeviceManagerNulTests()
    {
        _user = new User("user", "auth", "reset") { Id = _userId };
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = _userId });
            context.SaveChanges();
        }

        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUserById(_userId)).Returns(_user);
        _manager = new DeviceManager(CreateDbContextFactory(), userManager.Object);
    }

    [Theory]
    [InlineData("dev\0")]
    [InlineData("dev")]
    [InlineData("d\0ev")]
    public async Task GetDevices_DeviceIdWithOrWithoutNul_FindsTheDeviceCreatedWithANul(string deviceId)
    {
        await _manager.CreateDevice(new Device(_userId, "ap\0p", "1.\00", "na\0me", "dev\0"));

        var device = Assert.Single(_manager.GetDevices(new DeviceQuery { DeviceId = deviceId, UserId = _userId }).Items);

        Assert.Equal("dev", device.DeviceId);
        Assert.Equal("app", device.AppName);
        Assert.Equal("1.0", device.AppVersion);
        Assert.Equal("name", device.DeviceName);
    }

    [Fact]
    public async Task CreateDevice_ValuesWithNul_StoresThemWithoutIt()
    {
        await _manager.CreateDevice(new Device(_userId, "ap\0p", "1.\00", "na\0me", "dev\0"));

        using var context = CreateDbContext();
        var stored = Assert.Single(context.Devices);
        Assert.Equal(("dev", "app", "1.0", "name"), (stored.DeviceId, stored.AppName, stored.AppVersion, stored.DeviceName));
    }

    [Theory]
    [InlineData("dev\0")]
    [InlineData("dev")]
    public async Task GetDevice_DeviceIdWithOrWithoutNul_FindsTheDeviceCreatedWithANul(string deviceId)
    {
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", "dev\0"));

        var device = _manager.GetDevice(deviceId);

        Assert.Equal("dev", device?.Id);
    }

    [Fact]
    public async Task GetDevice_DeviceIdWithNul_FindsTheOptionsStoredUnderTheCleanedId()
    {
        await _manager.CreateDevice(new Device(_userId, "app", "1.0", "name", "dev"));
        await _manager.UpdateDeviceOptions("dev", "custom");

        var device = _manager.GetDevice("dev\0");

        Assert.Equal("custom", device?.CustomName);
    }

    [Fact]
    public void GetCapabilities_DeviceIdWithNul_FindsTheCapabilitiesSavedUnderTheCleanedId()
    {
        var capabilities = new ClientCapabilities();
        _manager.SaveCapabilities("dev", capabilities);

        Assert.Same(capabilities, _manager.GetCapabilities("de\0v"));
    }

    [Fact]
    public void SaveCapabilities_DeviceIdWithNul_SavesThemUnderTheCleanedId()
    {
        var capabilities = new ClientCapabilities();
        _manager.SaveCapabilities("dev\0", capabilities);

        Assert.Same(capabilities, _manager.GetCapabilities("dev"));
    }

    [Fact]
    public void CanAccessDevice_DeviceIdWithNul_MatchesTheEnabledDeviceStoredWithoutIt()
    {
        var user = new User("other", "auth", "reset");
        user.SetPermission(PermissionKind.EnableAllDevices, false);
        user.SetPreference(PreferenceKind.EnabledDevices, ["dev"]);
        _manager.SaveCapabilities("dev", new ClientCapabilities { SupportsPersistentIdentifier = true });

        Assert.True(_manager.CanAccessDevice(user, "de\0v"));
        Assert.False(_manager.CanAccessDevice(user, "other\0"));
    }

    [Fact]
    public async Task UpdateDeviceOptions_DeviceIdWithNulUpdatedTwice_UpdatesTheStoredOptions()
    {
        await _manager.UpdateDeviceOptions("device\0", "first");
        await _manager.UpdateDeviceOptions("device\0", "second");

        using var context = CreateDbContext();
        var options = Assert.Single(context.DeviceOptions);
        Assert.Equal("device", options.DeviceId);
        Assert.Equal("second", options.CustomName);
    }

    [Fact]
    public async Task GetDeviceOptions_DeviceIdWithNul_FindsTheOptionsStoredUnderTheCleanedId()
    {
        await _manager.UpdateDeviceOptions("device", "name");

        var options = _manager.GetDeviceOptions("dev\0ice");

        Assert.Equal("name", options?.CustomName);
    }

    [Theory]
    [InlineData("dev\0")]
    [InlineData("dev")]
    public void GetDevices_DeviceIdWithOrWithoutNul_FindsALegacyDeviceStoredWithANul(string deviceId)
    {
        var manager = CreateManagerWithLegacyDevice();

        var device = Assert.Single(manager.GetDevices(new DeviceQuery { DeviceId = deviceId, UserId = _userId }).Items);

        Assert.Equal("name", device.DeviceName);
    }

    [Theory]
    [InlineData("dev\0")]
    [InlineData("dev")]
    public void GetDevice_DeviceIdWithOrWithoutNul_FindsALegacyDeviceStoredWithANul(string deviceId)
    {
        var manager = CreateManagerWithLegacyDevice();

        var device = manager.GetDevice(deviceId);

        Assert.Equal("name", device?.Name);
        Assert.Equal("custom", device?.CustomName);
    }

    private DeviceManager CreateManagerWithLegacyDevice()
    {
        using (var context = CreateDbContext())
        {
            context.Devices.Add(new Device(_userId, "app", "1.0", "name", "dev"));
            context.DeviceOptions.Add(new DeviceOptions("dev") { CustomName = "custom" });
            context.SaveChanges();

            // Rows saved before NUL characters were removed keep them.
            context.Database.ExecuteSql($"UPDATE Devices SET DeviceId = 'dev' || char(0)");
            context.Database.ExecuteSql($"UPDATE DeviceOptions SET DeviceId = 'dev' || char(0)");
        }

        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUserById(_userId)).Returns(_user);
        return new DeviceManager(CreateDbContextFactory(), userManager.Object);
    }
}
