using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Devices
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly object _syncLock = new object();

        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        private Dictionary<string, DeviceInfo> _devices;

        public DeviceRepository(IApplicationPaths appPaths, IJsonSerializer json, ILogger logger, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _json = json;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        private string GetDevicesPath()
        {
            return Path.Combine(_appPaths.DataPath, "devices");
        }

        private string GetDevicePath(string id)
        {
            return Path.Combine(GetDevicesPath(), id.GetMD5().ToString("N"));
        }

        public Task SaveDevice(DeviceInfo device)
        {
            var path = Path.Combine(GetDevicePath(device.Id), "device.json");
            _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            lock (_syncLock)
            {
                _json.SerializeToFile(device, path);
                _devices[device.Id] = device;
            }
            return Task.FromResult(true);
        }

        public Task SaveCapabilities(string reportedId, ClientCapabilities capabilities)
        {
            var device = GetDevice(reportedId);

            if (device == null)
            {
                throw new ArgumentException("No device has been registed with id " + reportedId);
            }

            device.Capabilities = capabilities;
            SaveDevice(device);

            return Task.FromResult(true);
        }

        public ClientCapabilities GetCapabilities(string reportedId)
        {
            var device = GetDevice(reportedId);

            return device == null ? null : device.Capabilities;
        }

        public DeviceInfo GetDevice(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            return GetDevices()
                .FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<DeviceInfo> GetDevices()
        {
            lock (_syncLock)
            {
                if (_devices == null)
                {
                    _devices = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);

                    var devices = LoadDevices().ToList();
                    foreach (var device in devices)
                    {
                        _devices[device.Id] = device;
                    }
                }
                return _devices.Values.ToList();
            }
        }

        private IEnumerable<DeviceInfo> LoadDevices()
        {
            var path = GetDevicesPath();

            try
            {
                return _fileSystem
                    .GetFilePaths(path, true)
                    .Where(i => string.Equals(Path.GetFileName(i), "device.json", StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .Select(i =>
                    {
                        try
                        {
                            return _json.DeserializeFromFile<DeviceInfo>(i);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error reading {0}", ex, i);
                            return null;
                        }
                    })
                    .Where(i => i != null);
            }
            catch (IOException)
            {
                return new List<DeviceInfo>();
            }
        }

        public Task DeleteDevice(string id)
        {
            var path = GetDevicePath(id);

            lock (_syncLock)
            {
                try
                {
                    _fileSystem.DeleteDirectory(path, true);
                }
                catch (DirectoryNotFoundException)
                {
                }

                _devices = null;
            }

            return Task.FromResult(true);
        }

        public ContentUploadHistory GetCameraUploadHistory(string deviceId)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "camerauploads.json");

            lock (_syncLock)
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

        public void AddCameraUpload(string deviceId, LocalFileInfo file)
        {
            var path = Path.Combine(GetDevicePath(deviceId), "camerauploads.json");
            _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            lock (_syncLock)
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
                history.FilesUploaded.Add(file);

                _json.SerializeToFile(history, path);
            }
        }
    }
}
