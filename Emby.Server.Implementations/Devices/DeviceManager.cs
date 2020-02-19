#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Devices
{
    public class DeviceManager : IDeviceManager
    {
        private readonly IJsonSerializer _json;
        private readonly IUserManager _userManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localizationManager;

        private readonly IAuthenticationRepository _authRepo;

        public event EventHandler<GenericEventArgs<Tuple<string, DeviceOptions>>> DeviceOptionsUpdated;
        public event EventHandler<GenericEventArgs<CameraImageUploadInfo>> CameraImageUploaded;

        private readonly object _cameraUploadSyncLock = new object();
        private readonly object _capabilitiesSyncLock = new object();

        public DeviceManager(
            IAuthenticationRepository authRepo,
            IJsonSerializer json,
            ILibraryManager libraryManager,
            ILocalizationManager localizationManager,
            IUserManager userManager,
            IFileSystem fileSystem,
            ILibraryMonitor libraryMonitor,
            IServerConfigurationManager config)
        {
            _json = json;
            _userManager = userManager;
            _fileSystem = fileSystem;
            _libraryMonitor = libraryMonitor;
            _config = config;
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            _authRepo = authRepo;
        }


        private Dictionary<string, ClientCapabilities> _capabilitiesCache = new Dictionary<string, ClientCapabilities>(StringComparer.OrdinalIgnoreCase);
        public void SaveCapabilities(string deviceId, ClientCapabilities capabilities)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "capabilities.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            lock (_capabilitiesSyncLock)
            {
                _capabilitiesCache[deviceId] = capabilities;

                _json.SerializeToFile(capabilities, path);
            }
        }

        public void UpdateDeviceOptions(string deviceId, DeviceOptions options)
        {
            _authRepo.UpdateDeviceOptions(deviceId, options);

            if (DeviceOptionsUpdated != null)
            {
                DeviceOptionsUpdated(this, new GenericEventArgs<Tuple<string, DeviceOptions>>()
                {
                    Argument = new Tuple<string, DeviceOptions>(deviceId, options)
                });
            }
        }

        public DeviceOptions GetDeviceOptions(string deviceId)
        {
            return _authRepo.GetDeviceOptions(deviceId);
        }

        public ClientCapabilities GetCapabilities(string id)
        {
            lock (_capabilitiesSyncLock)
            {
                if (_capabilitiesCache.TryGetValue(id, out var result))
                {
                    return result;
                }

                var path = Path.Combine(GetDevicePath(id), "capabilities.json");
                try
                {
                    return _json.DeserializeFromFile<ClientCapabilities>(path) ?? new ClientCapabilities();
                }
                catch
                {
                }
            }

            return new ClientCapabilities();
        }

        public DeviceInfo GetDevice(string id)
        {
            return GetDevice(id, true);
        }

        private DeviceInfo GetDevice(string id, bool includeCapabilities)
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
                //UserId = query.UserId
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

        private string GetDevicesPath()
        {
            return Path.Combine(_config.ApplicationPaths.DataPath, "devices");
        }

        private string GetDevicePath(string id)
        {
            return Path.Combine(GetDevicesPath(), id.GetMD5().ToString("N", CultureInfo.InvariantCulture));
        }

        public ContentUploadHistory GetCameraUploadHistory(string deviceId)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "camerauploads.json");

            lock (_cameraUploadSyncLock)
            {
                try
                {
                    return _json.DeserializeFromFile<ContentUploadHistory>(path);
                }
                catch (IOException)
                {
                    return new ContentUploadHistory
                    {
                        DeviceId = deviceId
                    };
                }
            }
        }

        public async Task AcceptCameraUpload(string deviceId, Stream stream, LocalFileInfo file)
        {
            var device = GetDevice(deviceId, false);
            var uploadPathInfo = GetUploadPath(device);

            var path = uploadPathInfo.Item1;

            if (!string.IsNullOrWhiteSpace(file.Album))
            {
                path = Path.Combine(path, _fileSystem.GetValidFilename(file.Album));
            }

            path = Path.Combine(path, file.Name);
            path = Path.ChangeExtension(path, MimeTypes.ToExtension(file.MimeType) ?? "jpg");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            await EnsureLibraryFolder(uploadPathInfo.Item2, uploadPathInfo.Item3).ConfigureAwait(false);

            _libraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await stream.CopyToAsync(fs).ConfigureAwait(false);
                }

                AddCameraUpload(deviceId, file);
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(path, true);
            }

            if (CameraImageUploaded != null)
            {
                CameraImageUploaded?.Invoke(this, new GenericEventArgs<CameraImageUploadInfo>
                {
                    Argument = new CameraImageUploadInfo
                    {
                        Device = device,
                        FileInfo = file
                    }
                });
            }
        }

        private void AddCameraUpload(string deviceId, LocalFileInfo file)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "camerauploads.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            lock (_cameraUploadSyncLock)
            {
                ContentUploadHistory history;

                try
                {
                    history = _json.DeserializeFromFile<ContentUploadHistory>(path);
                }
                catch (IOException)
                {
                    history = new ContentUploadHistory
                    {
                        DeviceId = deviceId
                    };
                }

                history.DeviceId = deviceId;

                var list = history.FilesUploaded.ToList();
                list.Add(file);
                history.FilesUploaded = list.ToArray();

                _json.SerializeToFile(history, path);
            }
        }

        internal Task EnsureLibraryFolder(string path, string name)
        {
            var existingFolders = _libraryManager
                .RootFolder
                .Children
                .OfType<Folder>()
                .Where(i => _fileSystem.AreEqual(path, i.Path) || _fileSystem.ContainsSubPath(i.Path, path))
                .ToList();

            if (existingFolders.Count > 0)
            {
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(path);

            var libraryOptions = new LibraryOptions
            {
                PathInfos = new[] { new MediaPathInfo { Path = path } },
                EnablePhotos = true,
                EnableRealtimeMonitor = false,
                SaveLocalMetadata = true
            };

            if (string.IsNullOrWhiteSpace(name))
            {
                name = _localizationManager.GetLocalizedString("HeaderCameraUploads");
            }

            return _libraryManager.AddVirtualFolder(name, CollectionType.HomeVideos, libraryOptions, true);
        }

        private Tuple<string, string, string> GetUploadPath(DeviceInfo device)
        {
            var config = _config.GetUploadOptions();
            var path = config.CameraUploadPath;

            if (string.IsNullOrWhiteSpace(path))
            {
                path = DefaultCameraUploadsPath;
            }

            var topLibraryPath = path;

            if (config.EnableCameraUploadSubfolders)
            {
                path = Path.Combine(path, _fileSystem.GetValidFilename(device.Name));
            }

            return new Tuple<string, string, string>(path, topLibraryPath, null);
        }

        internal string GetUploadsPath()
        {
            var config = _config.GetUploadOptions();
            var path = config.CameraUploadPath;

            if (string.IsNullOrWhiteSpace(path))
            {
                path = DefaultCameraUploadsPath;
            }

            return path;
        }

        private string DefaultCameraUploadsPath => Path.Combine(_config.CommonApplicationPaths.DataPath, "camerauploads");

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

        private static bool CanAccessDevice(UserPolicy policy, string id)
        {
            if (policy.EnableAllDevices)
            {
                return true;
            }

            if (policy.IsAdministrator)
            {
                return true;
            }

            return policy.EnabledDevices.Contains(id, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class DeviceManagerEntryPoint : IServerEntryPoint
    {
        private readonly DeviceManager _deviceManager;
        private readonly IServerConfigurationManager _config;
        private ILogger _logger;

        public DeviceManagerEntryPoint(IDeviceManager deviceManager, IServerConfigurationManager config, ILogger logger)
        {
            _deviceManager = (DeviceManager)deviceManager;
            _config = config;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            if (!_config.Configuration.CameraUploadUpgraded && _config.Configuration.IsStartupWizardCompleted)
            {
                var path = _deviceManager.GetUploadsPath();

                if (Directory.Exists(path))
                {
                    try
                    {
                        await _deviceManager.EnsureLibraryFolder(path, null).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating camera uploads library");
                    }

                    _config.Configuration.CameraUploadUpgraded = true;
                    _config.SaveConfiguration();
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DeviceManagerEntryPoint() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class DevicesConfigStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
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
