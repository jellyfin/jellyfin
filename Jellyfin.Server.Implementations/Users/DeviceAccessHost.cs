using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Server.Implementations.Users;

/// <summary>
/// <see cref="IHostedService"/> responsible for managing user device permissions.
/// </summary>
public sealed class DeviceAccessHost : IHostedService
{
    private readonly IUserManager _userManager;
    private readonly IDeviceManager _deviceManager;
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceAccessHost"/> class.
    /// </summary>
    /// <param name="userManager">The <see cref="IUserManager"/>.</param>
    /// <param name="deviceManager">The <see cref="IDeviceManager"/>.</param>
    /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
    public DeviceAccessHost(IUserManager userManager, IDeviceManager deviceManager, ISessionManager sessionManager)
    {
        _userManager = userManager;
        _deviceManager = deviceManager;
        _sessionManager = sessionManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userManager.OnUserUpdated += OnUserUpdated;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userManager.OnUserUpdated -= OnUserUpdated;

        return Task.CompletedTask;
    }

    private async void OnUserUpdated(object? sender, GenericEventArgs<User> e)
    {
        var user = e.Argument;
        if (!user.HasPermission(PermissionKind.EnableAllDevices))
        {
            await UpdateDeviceAccess(user).ConfigureAwait(false);
        }
    }

    private async Task UpdateDeviceAccess(User user)
    {
        var existing = _deviceManager.GetDevices(new DeviceQuery
        {
            UserId = user.Id
        }).Items;

        foreach (var device in existing)
        {
            if (!string.IsNullOrEmpty(device.DeviceId) && !_deviceManager.CanAccessDevice(user, device.DeviceId))
            {
                await _sessionManager.Logout(device).ConfigureAwait(false);
            }
        }
    }
}
