using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Configuration;

namespace MediaBrowser.Server.Implementations.Devices
{
    public class DeviceManager : IDeviceManager
    {
        private readonly IDeviceRepository _repo;
        private readonly IUserManager _userManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly INetworkManager _network;

        public event EventHandler<GenericEventArgs<CameraImageUploadInfo>> CameraImageUploaded;

        /// <summary>
        /// Occurs when [device options updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<DeviceInfo>> DeviceOptionsUpdated;

        public DeviceManager(IDeviceRepository repo, IUserManager userManager, IFileSystem fileSystem, ILibraryMonitor libraryMonitor, IServerConfigurationManager config, ILogger logger, INetworkManager network)
        {
            _repo = repo;
            _userManager = userManager;
            _fileSystem = fileSystem;
            _libraryMonitor = libraryMonitor;
            _config = config;
            _logger = logger;
            _network = network;
        }

        public async Task<DeviceInfo> RegisterDevice(string reportedId, string name, string appName, string appVersion, string usedByUserId)
        {
            if (string.IsNullOrWhiteSpace(reportedId))
            {
                throw new ArgumentNullException("reportedId");
            }

            var device = GetDevice(reportedId) ?? new DeviceInfo
            {
                Id = reportedId
            };

            device.ReportedName = name;
            device.AppName = appName;
            device.AppVersion = appVersion;

            if (!string.IsNullOrWhiteSpace(usedByUserId))
            {
                var user = _userManager.GetUserById(usedByUserId);

                device.LastUserId = user.Id.ToString("N");
                device.LastUserName = user.Name;
            }

            device.DateLastModified = DateTime.UtcNow;

            await _repo.SaveDevice(device).ConfigureAwait(false);

            return device;
        }

        public Task SaveCapabilities(string reportedId, ClientCapabilities capabilities)
        {
            return _repo.SaveCapabilities(reportedId, capabilities);
        }

        public ClientCapabilities GetCapabilities(string reportedId)
        {
            return _repo.GetCapabilities(reportedId);
        }

        public DeviceInfo GetDevice(string id)
        {
            return _repo.GetDevice(id);
        }

        public QueryResult<DeviceInfo> GetDevices(DeviceQuery query)
        {
            IEnumerable<DeviceInfo> devices = _repo.GetDevices().OrderByDescending(i => i.DateLastModified);

            if (query.SupportsContentUploading.HasValue)
            {
                var val = query.SupportsContentUploading.Value;

                devices = devices.Where(i => GetCapabilities(i.Id).SupportsContentUploading == val);
            }

            if (query.SupportsSync.HasValue)
            {
                var val = query.SupportsSync.Value;

                devices = devices.Where(i => GetCapabilities(i.Id).SupportsSync == val);
            }

            if (query.SupportsPersistentIdentifier.HasValue)
            {
                var val = query.SupportsPersistentIdentifier.Value;

                devices = devices.Where(i =>
                {
                    var caps = GetCapabilities(i.Id);
                    var deviceVal = caps.SupportsPersistentIdentifier;
                    return deviceVal == val;
                });
            }

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                devices = devices.Where(i => CanAccessDevice(query.UserId, i.Id));
            }

            var array = devices.ToArray();
            return new QueryResult<DeviceInfo>
            {
                Items = array,
                TotalRecordCount = array.Length
            };
        }

        public Task DeleteDevice(string id)
        {
            return _repo.DeleteDevice(id);
        }

        public ContentUploadHistory GetCameraUploadHistory(string deviceId)
        {
            return _repo.GetCameraUploadHistory(deviceId);
        }

        public async Task AcceptCameraUpload(string deviceId, Stream stream, LocalFileInfo file)
        {
            var device = GetDevice(deviceId);
            var path = GetUploadPath(device);

            if (!string.IsNullOrWhiteSpace(file.Album))
            {
                path = Path.Combine(path, _fileSystem.GetValidFilename(file.Album));
            }

            path = Path.Combine(path, file.Name);
            path = Path.ChangeExtension(path, MimeTypes.ToExtension(file.MimeType) ?? "jpg");

            _libraryMonitor.ReportFileSystemChangeBeginning(path);

            _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            try
            {
                using (var fs = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await stream.CopyToAsync(fs).ConfigureAwait(false);
                }

                _repo.AddCameraUpload(deviceId, file);
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(path, true);
            }

            if (CameraImageUploaded != null)
            {
                EventHelper.FireEventIfNotNull(CameraImageUploaded, this, new GenericEventArgs<CameraImageUploadInfo>
                {
                    Argument = new CameraImageUploadInfo
                    {
                        Device = device,
                        FileInfo = file
                    }
                }, _logger);
            }
        }

        private string GetUploadPath(DeviceInfo device)
        {
            if (!string.IsNullOrWhiteSpace(device.CameraUploadPath))
            {
                return device.CameraUploadPath;
            }

            var config = _config.GetUploadOptions();
            if (!string.IsNullOrWhiteSpace(config.CameraUploadPath))
            {
                return config.CameraUploadPath;
            }

            var path = DefaultCameraUploadsPath;

            if (config.EnableCameraUploadSubfolders)
            {
                path = Path.Combine(path, _fileSystem.GetValidFilename(device.Name));
            }

            return path;
        }

        private string DefaultCameraUploadsPath
        {
            get { return Path.Combine(_config.CommonApplicationPaths.DataPath, "camerauploads"); }
        }

        public async Task UpdateDeviceInfo(string id, DeviceOptions options)
        {
            var device = GetDevice(id);

            device.CustomName = options.CustomName;
            device.CameraUploadPath = options.CameraUploadPath;

            await _repo.SaveDevice(device).ConfigureAwait(false);

            EventHelper.FireEventIfNotNull(DeviceOptionsUpdated, this, new GenericEventArgs<DeviceInfo>(device), _logger);
        }

        public bool CanAccessDevice(string userId, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentNullException("deviceId");
            }

            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                throw new ArgumentException("user not found");
            }

            if (!CanAccessDevice(user.Policy, deviceId))
            {
                var capabilities = GetCapabilities(deviceId);

                if (capabilities != null && capabilities.SupportsPersistentIdentifier)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanAccessDevice(UserPolicy policy, string id)
        {
            if (policy.EnableAllDevices)
            {
                return true;
            }

            if (policy.IsAdministrator)
            {
                return true;
            }

            return ListHelper.ContainsIgnoreCase(policy.EnabledDevices, id);
        }
    }

    public class DevicesConfigStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                     Key = "devices",
                     ConfigurationType = typeof(DevicesOptions)
                }
            };
        }
    }

    public static class UploadConfigExtension
    {
        public static DevicesOptions GetUploadOptions(this IConfigurationManager config)
        {
            return config.GetConfiguration<DevicesOptions>("devices");
        }
    }
}