using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Devices
{
    public class DeviceManager : IDeviceManager
    {
        private readonly IDeviceRepository _repo;
        private readonly IUserManager _userManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly IConfigurationManager _config;

        public DeviceManager(IDeviceRepository repo, IUserManager userManager, IFileSystem fileSystem, ILibraryMonitor libraryMonitor, IConfigurationManager config)
        {
            _repo = repo;
            _userManager = userManager;
            _fileSystem = fileSystem;
            _libraryMonitor = libraryMonitor;
            _config = config;
        }

        public Task RegisterDevice(string reportedId, string name, string usedByUserId)
        {
            var device = GetDevice(reportedId) ?? new DeviceInfo
            {
                Id = reportedId
            };

            device.Name = name;

            if (!string.IsNullOrWhiteSpace(usedByUserId))
            {
                var user = _userManager.GetUserById(usedByUserId);

                device.LastUserId = user.Id.ToString("N");
                device.LastUserName = user.Name;
            }

            device.DateLastModified = DateTime.UtcNow;

            return _repo.SaveDevice(device);
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

        public IEnumerable<DeviceInfo> GetDevices()
        {
            return _repo.GetDevices().OrderByDescending(i => i.DateLastModified);
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
            var path = GetUploadPath(deviceId);

            if (!string.IsNullOrWhiteSpace(file.Album))
            {
                path = Path.Combine(path, _fileSystem.GetValidFilename(file.Album));
            }

            Directory.CreateDirectory(path);

            path = Path.Combine(path, file.Name);

            _libraryMonitor.ReportFileSystemChangeBeginning(path);

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
        }

        private string GetUploadPath(string deviceId)
        {
            var config = _config.GetUploadOptions();

            if (!string.IsNullOrWhiteSpace(config.CameraUploadPath))
            {
                return config.CameraUploadPath;
            }

            return Path.Combine(_config.CommonApplicationPaths.DataPath, "camerauploads");
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
