using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Devices
{
    /// <summary>
    /// Manages the creation, updating, and retrieval of devices.
    /// </summary>
    public class DeviceManager : IDeviceManager
    {
        private readonly IDbContextFactory<JellyfinDb> _dbProvider;
        private readonly IUserManager _userManager;
        private readonly ConcurrentDictionary<string, ClientCapabilities> _capabilitiesMap = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceManager"/> class.
        /// </summary>
        /// <param name="dbProvider">The database provider.</param>
        /// <param name="userManager">The user manager.</param>
        public DeviceManager(IDbContextFactory<JellyfinDb> dbProvider, IUserManager userManager)
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
        public async Task UpdateDeviceOptions(string deviceId, string deviceName)
        {
            DeviceOptions? deviceOptions;
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                deviceOptions = await dbContext.DeviceOptions.AsQueryable().FirstOrDefaultAsync(dev => dev.DeviceId == deviceId).ConfigureAwait(false);
                if (deviceOptions == null)
                {
                    deviceOptions = new DeviceOptions(deviceId);
                    dbContext.DeviceOptions.Add(deviceOptions);
                }

                deviceOptions.CustomName = deviceName;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            DeviceOptionsUpdated?.Invoke(this, new GenericEventArgs<Tuple<string, DeviceOptions>>(new Tuple<string, DeviceOptions>(deviceId, deviceOptions)));
        }

        /// <inheritdoc />
        public async Task<Device> CreateDevice(Device device)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.Devices.Add(device);

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return device;
        }

        /// <inheritdoc />
        public async Task<DeviceOptions> GetDeviceOptions(string deviceId)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            DeviceOptions? deviceOptions;
            await using (dbContext.ConfigureAwait(false))
            {
                deviceOptions = await dbContext.DeviceOptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId)
                    .ConfigureAwait(false);
            }

            return deviceOptions ?? new DeviceOptions(deviceId);
        }

        /// <inheritdoc />
        public ClientCapabilities GetCapabilities(string deviceId)
        {
            return _capabilitiesMap.TryGetValue(deviceId, out ClientCapabilities? result)
                ? result
                : new ClientCapabilities();
        }

        /// <inheritdoc />
        public async Task<DeviceInfo?> GetDevice(string id)
        {
            Device? device;
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                device = await dbContext.Devices
                    .Where(d => d.DeviceId == id)
                    .OrderByDescending(d => d.DateLastActivity)
                    .Include(d => d.User)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }

            var deviceInfo = device == null ? null : ToDeviceInfo(device);

            return deviceInfo;
        }

        /// <inheritdoc />
        public async Task<QueryResult<Device>> GetDevices(DeviceQuery query)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var devices = dbContext.Devices.AsQueryable();

                if (query.UserId.HasValue)
                {
                    devices = devices.Where(device => device.UserId.Equals(query.UserId.Value));
                }

                if (query.DeviceId != null)
                {
                    devices = devices.Where(device => device.DeviceId == query.DeviceId);
                }

                if (query.AccessToken != null)
                {
                    devices = devices.Where(device => device.AccessToken == query.AccessToken);
                }

                var count = await devices.CountAsync().ConfigureAwait(false);

                if (query.Skip.HasValue)
                {
                    devices = devices.Skip(query.Skip.Value);
                }

                if (query.Limit.HasValue)
                {
                    devices = devices.Take(query.Limit.Value);
                }

                return new QueryResult<Device>(query.Skip, count, await devices.ToListAsync().ConfigureAwait(false));
            }
        }

        /// <inheritdoc />
        public async Task<QueryResult<DeviceInfo>> GetDeviceInfos(DeviceQuery query)
        {
            var devices = await GetDevices(query).ConfigureAwait(false);

            return new QueryResult<DeviceInfo>(
                devices.StartIndex,
                devices.TotalRecordCount,
                devices.Items.Select(device => ToDeviceInfo(device)).ToList());
        }

        /// <inheritdoc />
        public async Task<QueryResult<DeviceInfo>> GetDevicesForUser(Guid? userId, bool? supportsSync)
        {
            IAsyncEnumerable<Device> sessions;
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                sessions = dbContext.Devices
                    .Include(d => d.User)
                    .OrderByDescending(d => d.DateLastActivity)
                    .ThenBy(d => d.DeviceId)
                    .AsAsyncEnumerable();

                if (supportsSync.HasValue)
                {
                    sessions = sessions.Where(i => GetCapabilities(i.DeviceId).SupportsSync == supportsSync.Value);
                }

                if (userId.HasValue)
                {
                    var user = _userManager.GetUserById(userId.Value);

                    sessions = sessions.Where(i => CanAccessDevice(user, i.DeviceId));
                }

                var array = await sessions.Select(device => ToDeviceInfo(device)).ToArrayAsync().ConfigureAwait(false);

                return new QueryResult<DeviceInfo>(array);
            }
        }

        /// <inheritdoc />
        public async Task DeleteDevice(Device device)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.Devices.Remove(device);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public bool CanAccessDevice(User user, string deviceId)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (user.HasPermission(PermissionKind.EnableAllDevices) || user.HasPermission(PermissionKind.IsAdministrator))
            {
                return true;
            }

            return user.GetPreference(PreferenceKind.EnabledDevices).Contains(deviceId, StringComparison.OrdinalIgnoreCase)
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
