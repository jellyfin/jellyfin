#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;

namespace Emby.Server.Implementations.Devices
{
    public class DeviceManager : IDeviceManager
    {
        private readonly IUserManager _userManager;
        private readonly IAuthenticationRepository _authRepo;
        private readonly ConcurrentDictionary<string, ClientCapabilities> _capabilitiesMap = new ();

        public DeviceManager(IAuthenticationRepository authRepo, IUserManager userManager)
        {
            _userManager = userManager;
            _authRepo = authRepo;
        }

        public event EventHandler<GenericEventArgs<Tuple<string, DeviceOptions>>> DeviceOptionsUpdated;

        public void SaveCapabilities(string deviceId, ClientCapabilities capabilities)
        {
            _capabilitiesMap[deviceId] = capabilities;
        }

        public void UpdateDeviceOptions(string deviceId, DeviceOptions options)
        {
            _authRepo.UpdateDeviceOptions(deviceId, options);

            DeviceOptionsUpdated?.Invoke(this, new GenericEventArgs<Tuple<string, DeviceOptions>>(new Tuple<string, DeviceOptions>(deviceId, options)));
        }

        public DeviceOptions GetDeviceOptions(string deviceId)
        {
            return _authRepo.GetDeviceOptions(deviceId);
        }

        public ClientCapabilities GetCapabilities(string id)
        {
            return _capabilitiesMap.TryGetValue(id, out ClientCapabilities result)
                ? result
                : new ClientCapabilities();
        }

        public DeviceInfo GetDevice(string id)
        {
            var session = _authRepo.Get(new AuthenticationInfoQuery
            {
                DeviceId = id
            }).Items.FirstOrDefault();

            var device = session == null ? null : ToDeviceInfo(session);

            return device;
        }

        public QueryResult<DeviceInfo> GetDevices(DeviceQuery query)
        {
            IEnumerable<AuthenticationInfo> sessions = _authRepo.Get(new AuthenticationInfoQuery
            {
                // UserId = query.UserId
                HasUser = true
            }).Items;

            // TODO: DeviceQuery doesn't seem to be used from client. Not even Swagger.
            if (query.SupportsSync.HasValue)
            {
                var val = query.SupportsSync.Value;

                sessions = sessions.Where(i => GetCapabilities(i.DeviceId).SupportsSync == val);
            }

            if (!query.UserId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(query.UserId);

                sessions = sessions.Where(i => CanAccessDevice(user, i.DeviceId));
            }

            var array = sessions.Select(ToDeviceInfo).ToArray();

            return new QueryResult<DeviceInfo>(array);
        }

        private DeviceInfo ToDeviceInfo(AuthenticationInfo authInfo)
        {
            var caps = GetCapabilities(authInfo.DeviceId);

            return new DeviceInfo
            {
                AppName = authInfo.AppName,
                AppVersion = authInfo.AppVersion,
                Id = authInfo.DeviceId,
                LastUserId = authInfo.UserId,
                LastUserName = authInfo.UserName,
                Name = authInfo.DeviceName,
                DateLastActivity = authInfo.DateLastActivity,
                IconUrl = caps?.IconUrl
            };
        }

        public bool CanAccessDevice(User user, string deviceId)
        {
            if (user == null)
            {
                throw new ArgumentException("user not found");
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (user.HasPermission(PermissionKind.EnableAllDevices) || user.HasPermission(PermissionKind.IsAdministrator))
            {
                return true;
            }

            if (!user.GetPreference(PreferenceKind.EnabledDevices).Contains(deviceId, StringComparer.OrdinalIgnoreCase))
            {
                var capabilities = GetCapabilities(deviceId);

                if (capabilities != null && capabilities.SupportsPersistentIdentifier)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
