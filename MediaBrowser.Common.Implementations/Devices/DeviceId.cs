using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Text;
using CommonIO;

namespace MediaBrowser.Common.Implementations.Devices
{
    public class DeviceId
    {
        private readonly IApplicationPaths _appPaths;
		private readonly ILogger _logger;
		private readonly IFileSystem _fileSystem;

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

				_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                lock (_syncLock)
                {
                    _fileSystem.WriteAllText(path, id, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error writing to file", ex);
            }
        }

        private string GetNewId()
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

        public DeviceId(IApplicationPaths appPaths, ILogger logger, IFileSystem fileSystem)
        {
			if (fileSystem == null) {
				throw new ArgumentNullException ("fileSystem");
			}

            _appPaths = appPaths;
            _logger = logger;
			_fileSystem = fileSystem;
        }

        public string Value
        {
            get { return _id ?? (_id = GetDeviceId()); }
        }
    }
}
