#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Devices
{
    public class DeviceId
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<DeviceId> _logger;
        private readonly Lock _syncLock = new();

        private string? _id;

        public DeviceId(IApplicationPaths appPaths, ILogger<DeviceId> logger)
        {
            _appPaths = appPaths;
            _logger = logger;
        }

        public string Value => _id ??= GetDeviceId();

        private string CachePath => Path.Combine(_appPaths.DataPath, "device.txt");

        private string? GetCachedId()
        {
            try
            {
                lock (_syncLock)
                {
                    var value = File.ReadAllText(CachePath, Encoding.UTF8);

                    if (Guid.TryParse(value, out _))
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

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Path can't be a root directory."));

                lock (_syncLock)
                {
                    File.WriteAllText(path, id, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to file");
            }
        }

        private static string GetNewId()
        {
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
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
    }
}
