using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Users;

/// <summary>
/// <see cref="IHostedService"/> responsible for managing user device permissions.
/// </summary>
public sealed class DeviceAccessHost : IHostedService
{
    private readonly IUserManager _userManager;
    private readonly IDeviceManager _deviceManager;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<DeviceAccessHost> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceAccessHost"/> class.
    /// </summary>
    /// <param name="userManager">The <see cref="IUserManager"/>.</param>
    /// <param name="deviceManager">The <see cref="IDeviceManager"/>.</param>
    /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    public DeviceAccessHost(
        IUserManager userManager,
        IDeviceManager deviceManager,
        ISessionManager sessionManager,
        ILogger<DeviceAccessHost> logger)
    {
        _userManager = userManager;
        _deviceManager = deviceManager;
        _sessionManager = sessionManager;
        _logger = logger;
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

        // This handler is async void, so an escaping exception would terminate the process.
        try
        {
            if (!user.HasPermission(PermissionKind.EnableAllDevices))
            {
                await UpdateDeviceAccess(user).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device access for user {UserId}", user.Id);
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
