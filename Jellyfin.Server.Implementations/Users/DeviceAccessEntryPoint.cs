#pragma warning disable CS1591

using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Users
{
    public sealed class DeviceAccessEntryPoint : IHandleMessages<UserUpdatedEventArgs>
    {
        private readonly IAuthenticationRepository _authRepo;
        private readonly IDeviceManager _deviceManager;
        private readonly ISessionManager _sessionManager;

        public DeviceAccessEntryPoint(IAuthenticationRepository authRepo, IDeviceManager deviceManager, ISessionManager sessionManager)
        {
            _authRepo = authRepo;
            _deviceManager = deviceManager;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public Task Handle(UserUpdatedEventArgs e)
        {
            var user = e.Argument;
            if (!user.HasPermission(PermissionKind.EnableAllDevices))
            {
                UpdateDeviceAccess(user);
            }

            return Task.CompletedTask;
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
