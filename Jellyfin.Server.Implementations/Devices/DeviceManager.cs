using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Devices
{
    public class DeviceManager : IDeviceManager
    {
        private readonly JellyfinDbProvider _dbProvider;
        private readonly IUserManager _userManager;
        private readonly ConcurrentDictionary<string, ClientCapabilities> _capabilitiesMap = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceManager"/> class.
        /// </summary>
        /// <param name="dbProvider">The database provider.</param>
        /// <param name="userManager">The user manager.</param>
        public DeviceManager(JellyfinDbProvider dbProvider, IUserManager userManager)
        {
            _dbProvider = dbProvider;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<Tuple<string, DeviceOptions>>>? DeviceOptionsUpdated;

        /// <inheritdoc />
        public void SaveCapabilities(string deviceId, ClientCapabilities capabilities)
        {
            _capabilitiesMap[deviceId] = capabilities;
        }

        /// <inheritdoc />
        public async Task UpdateDeviceOptions(string deviceId, DeviceOptions options)
        {
            await using var dbContext = _dbProvider.CreateContext();
            await dbContext.Database
                .ExecuteSqlRawAsync($"UPDATE [DeviceOptions] SET [CustomName] = ${options.CustomName}")
                .ConfigureAwait(false);

            DeviceOptionsUpdated?.Invoke(this, new GenericEventArgs<Tuple<string, DeviceOptions>>(new Tuple<string, DeviceOptions>(deviceId, options)));
        }

        /// <inheritdoc />
        public async Task<DeviceOptions?> GetDeviceOptions(string deviceId)
        {
            await using var dbContext = _dbProvider.CreateContext();
            return await dbContext.DeviceOptions
                .AsQueryable()
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ClientCapabilities GetCapabilities(string id)
        {
            return _capabilitiesMap.TryGetValue(id, out ClientCapabilities? result)
                ? result
                : new ClientCapabilities();
        }

        /// <inheritdoc />
        public async Task<DeviceInfo?> GetDevice(string id)
        {
            await using var dbContext = _dbProvider.CreateContext();
            var device = await dbContext.Devices
                .AsQueryable()
                .Where(d => d.DeviceId == id)
                .OrderByDescending(d => d.DateLastActivity)
                .Include(d => d.User)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            var deviceInfo = device == null ? null : ToDeviceInfo(device);

            return deviceInfo;
        }

        /// <inheritdoc />
        public async Task<QueryResult<DeviceInfo>> GetDevices(DeviceQuery query)
        {
            await using var dbContext = _dbProvider.CreateContext();
            var sessions = dbContext.Devices
                .AsQueryable()
                .OrderBy(d => d.DeviceId)
                .ThenByDescending(d => d.DateLastActivity)
                .AsAsyncEnumerable();

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

            var array = await sessions.Select(ToDeviceInfo).ToArrayAsync();

            return new QueryResult<DeviceInfo>(array);
        }

        /// <inheritdoc />
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

            return user.GetPreference(PreferenceKind.EnabledDevices).Contains(deviceId, StringComparer.OrdinalIgnoreCase)
                   || !GetCapabilities(deviceId).SupportsPersistentIdentifier;
        }

        private DeviceInfo ToDeviceInfo(Device authInfo)
        {
            var caps = GetCapabilities(authInfo.DeviceId);

            return new DeviceInfo
            {
                AppName = authInfo.AppName,
                AppVersion = authInfo.AppVersion,
                Id = authInfo.DeviceId,
                LastUserId = authInfo.UserId,
                LastUserName = authInfo.User.Username,
                Name = authInfo.DeviceName,
                DateLastActivity = authInfo.DateLastActivity,
                IconUrl = caps.IconUrl
            };
        }
    }
}
