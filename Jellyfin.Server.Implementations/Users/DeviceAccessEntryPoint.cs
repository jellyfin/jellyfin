#pragma warning disable CS1591

using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Server.Implementations.Users
{
    public sealed class DeviceAccessEntryPoint : IServerEntryPoint
    {
        private readonly IUserManager _userManager;
        private readonly IDeviceManager _deviceManager;
        private readonly ISessionManager _sessionManager;

        public DeviceAccessEntryPoint(IUserManager userManager, IDeviceManager deviceManager, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _deviceManager = deviceManager;
            _sessionManager = sessionManager;
        }

        public Task RunAsync()
        {
            _userManager.OnUserUpdated += OnUserUpdated;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
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
            var existing = (await _deviceManager.GetDevices(new DeviceQuery
            {
                UserId = user.Id
            }).ConfigureAwait(false)).Items;

            foreach (var device in existing)
            {
                if (!string.IsNullOrEmpty(device.DeviceId) && !_deviceManager.CanAccessDevice(user, device.DeviceId))
                {
                    await _sessionManager.Logout(device).ConfigureAwait(false);
                }
            }
        }
    }
}
