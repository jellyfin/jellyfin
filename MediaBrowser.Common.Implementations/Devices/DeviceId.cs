using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Text;

namespace MediaBrowser.Common.Implementations.Devices
{
    public class DeviceId
    {
        private readonly IApplicationPaths _appPaths;
        private readonly INetworkManager _networkManager;
        private readonly ILogger _logger;

        private readonly object _syncLock = new object();

        private string CachePath
        {
            get { return Path.Combine(_appPaths.DataPath, "device.txt"); }
        }

        private string GetCachedId()
        {
            try
            {
                lock (_syncLock)
                {
                    var value = File.ReadAllText(CachePath, Encoding.UTF8);

                    Guid guid;
                    if (Guid.TryParse(value, out guid))
                    {
                        return value;
                    }

                    _logger.Error("Invalid value found in device id file");
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
                _logger.ErrorException("Error reading file", ex);
            }

            return null;
        }

        private void SaveId(string id)
        {
            try
            {
                var path = CachePath;

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                lock (_syncLock)
                {
                    File.WriteAllText(path, id, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error writing to file", ex);
            }
        }

        private string GetNewId()
        {
            // When generating an Id, base it off of the app path + mac address
            // But we can't fail here, so if we can't get the mac address then just use a random guid

            string mac;

            try
            {
                mac = _networkManager.GetMacAddress();
            }
            catch
            {
                mac = Guid.NewGuid().ToString("N");
            }

            mac += "-" + _appPaths.ApplicationPath;

            return mac.GetMD5().ToString("N");
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

        public DeviceId(IApplicationPaths appPaths, ILogger logger, INetworkManager networkManager)
        {
            _appPaths = appPaths;
            _logger = logger;
            _networkManager = networkManager;
        }

        public string Value
        {
            get { return _id ?? (_id = GetDeviceId()); }
        }
    }
}
