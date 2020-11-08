#nullable enable
#pragma warning disable CS1591

using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Server.Implementations.Users
{
    public sealed class DeviceAccessEntryPoint : IServerEntryPoint
    {
        private readonly IUserManager _userManager;
        private readonly IAuthenticationRepository _authRepo;
        private readonly IDeviceManager _deviceManager;
        private readonly ISessionManager _sessionManager;

        public DeviceAccessEntryPoint(IUserManager userManager, IAuthenticationRepository authRepo, IDeviceManager deviceManager, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _authRepo = authRepo;
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

        private void OnUserUpdated(object? sender, GenericEventArgs<User> e)
        {
            var user = e.Argument;
            if (!user.HasPermission(PermissionKind.EnableAllDevices))
            {
                UpdateDeviceAccess(user);
            }
        }

        private void UpdateDeviceAccess(User user)
        {
            var existing = _authRepo.Get(new AuthenticationInfoQuery
            {
                UserId = user.Id
            }).Items;

            foreach (var authInfo in existing)
            {
                if (!string.IsNullOrEmpty(authInfo.DeviceId) && !_deviceManager.CanAccessDevice(user, authInfo.DeviceId))
                {
                    _sessionManager.Logout(authInfo);
                }
            }
        }
    }
}
