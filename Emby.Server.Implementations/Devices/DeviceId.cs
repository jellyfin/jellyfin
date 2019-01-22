using System;
using System.IO;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Devices
{
    public class DeviceId
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        private readonly object _syncLock = new object();

        private string CachePath => Path.Combine(_appPaths.DataPath, "device.txt");

        private string GetCachedId()
        {
            try
            {
                lock (_syncLock)
                {
                    var value = File.ReadAllText(CachePath, Encoding.UTF8);

                    if (Guid.TryParse(value, out var guid))
                    {
                        return value;
                    }

                    _logger.LogError("Invalid value found in device id file");
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file");
            }

            return null;
        }

        private void SaveId(string id)
        {
            try
            {
                var path = CachePath;

                _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(path));

                lock (_syncLock)
                {
                    _fileSystem.WriteAllText(path, id, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to file");
            }
        }

        private static string GetNewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private string GetDeviceId()
        {
            var id = GetCachedId();

            if (string.IsNullOrWhiteSpace(id))
            {
                id = GetNewId();
                SaveId(id);
            }

            return id;
        }

        private string _id;

        public DeviceId(
            IApplicationPaths appPaths,
            ILoggerFactory loggerFactory,
            IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _appPaths = appPaths;
            _logger = loggerFactory.CreateLogger("SystemId");
            _fileSystem = fileSystem;
        }

        public string Value => _id ?? (_id = GetDeviceId());
    }
}
