using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Security
{
    internal class MBLicenseFile
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoProvider _cryptographyProvider;

        public string RegKey
        {
            get { return _regKey; }
            set
            {
                if (value != _regKey)
                {
                    //if key is changed - clear out our saved validations
                    _updateRecords.Clear();
                    _regKey = value;
                }
            }
        }

        private string Filename
        {
            get
            {
                return Path.Combine(_appPaths.ConfigurationDirectoryPath, "mb.lic");
            }
        }

        private readonly ConcurrentDictionary<Guid, DateTime> _updateRecords = new ConcurrentDictionary<Guid, DateTime>();
        private readonly object _fileLock = new object();
        private string _regKey;

        public MBLicenseFile(IApplicationPaths appPaths, IFileSystem fileSystem, ICryptoProvider cryptographyProvider)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _cryptographyProvider = cryptographyProvider;

            Load();
        }

        private void SetUpdateRecord(Guid key, DateTime value)
        {
            _updateRecords.AddOrUpdate(key, value, (k, v) => value);
        }

        private Guid GetKey(string featureId)
        {
            return new Guid(_cryptographyProvider.ComputeMD5(Encoding.Unicode.GetBytes(featureId)));
        }

        public void AddRegCheck(string featureId)
        {
            var key = GetKey(featureId);
            var value = DateTime.UtcNow;

            SetUpdateRecord(key, value);
            Save();
        }

        public void RemoveRegCheck(string featureId)
        {
            var key = GetKey(featureId);
            DateTime val;

            _updateRecords.TryRemove(key, out val);

            Save();
        }

        public DateTime LastChecked(string featureId)
        {
            var key = GetKey(featureId);
            DateTime last;
            _updateRecords.TryGetValue(key, out last);

            // guard agains people just putting a large number in the file
            return last < DateTime.UtcNow ? last : DateTime.MinValue;
        }

        private void Load()
        {
            string[] contents = null;
            var licenseFile = Filename;
            lock (_fileLock)
            {
                try
                {
                    contents = _fileSystem.ReadAllLines(licenseFile);
                }
                catch (FileNotFoundException)
                {
                    lock (_fileLock)
                    {
                        _fileSystem.WriteAllBytes(licenseFile, new byte[] {});
                    }
                }
                catch (IOException)
                {
                    lock (_fileLock)
                    {
                        _fileSystem.WriteAllBytes(licenseFile, new byte[] { });
                    }
                }
            }
            if (contents != null && contents.Length > 0)
            {
                //first line is reg key
                RegKey = contents[0];

                //next is legacy key
                if (contents.Length > 1)
                {
                    // Don't need this anymore
                }

                //the rest of the lines should be pairs of features and timestamps
                for (var i = 2; i < contents.Length; i = i + 2)
                {
                    var line = contents[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    Guid feat;
                    if (Guid.TryParse(line, out feat))
                    {
                        SetUpdateRecord(feat, new DateTime(Convert.ToInt64(contents[i + 1])));
                    }
                }
            }
        }

        public void Save()
        {
            //build our array
            var lines = new List<string>
            {
                RegKey, 

                // Legacy key
                string.Empty
            };

            foreach (var pair in _updateRecords
                .ToList())
            {
                lines.Add(pair.Key.ToString());
                lines.Add(pair.Value.Ticks.ToString(CultureInfo.InvariantCulture));
            }

            var licenseFile = Filename;
            _fileSystem.CreateDirectory(Path.GetDirectoryName(licenseFile));
            lock (_fileLock)
            {
                _fileSystem.WriteAllLines(licenseFile, lines);
            }
        }
    }
}
