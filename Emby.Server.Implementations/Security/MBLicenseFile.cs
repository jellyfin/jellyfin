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

        private readonly ConcurrentDictionary<Guid, FeatureRegInfo> _updateRecords = new ConcurrentDictionary<Guid, FeatureRegInfo>();
        private readonly object _fileLock = new object();
        private string _regKey;

        public MBLicenseFile(IApplicationPaths appPaths, IFileSystem fileSystem, ICryptoProvider cryptographyProvider)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _cryptographyProvider = cryptographyProvider;

            Load();
        }

        private void SetUpdateRecord(Guid key, FeatureRegInfo value)
        {
            _updateRecords.AddOrUpdate(key, value, (k, v) => value);
        }

        private Guid GetKey(string featureId)
        {
            return new Guid(_cryptographyProvider.ComputeMD5(Encoding.Unicode.GetBytes(featureId)));
        }

        public void AddRegCheck(string featureId, DateTime expirationDate)
        {
            var key = GetKey(featureId);
            var value = new FeatureRegInfo
            {
                ExpirationDate = expirationDate,
                LastChecked = DateTime.UtcNow
            };

            SetUpdateRecord(key, value);
            Save();
        }

        public void RemoveRegCheck(string featureId)
        {
            var key = GetKey(featureId);
            FeatureRegInfo val;

            _updateRecords.TryRemove(key, out val);

            Save();
        }

        public FeatureRegInfo GetRegInfo(string featureId)
        {
            var key = GetKey(featureId);
            FeatureRegInfo info = null;
            _updateRecords.TryGetValue(key, out info);

            if (info == null)
            {
                return null;
            }

            // guard agains people just putting a large number in the file
            return info.LastChecked < DateTime.UtcNow ? info : null;
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
                        _fileSystem.WriteAllBytes(licenseFile, new byte[] { });
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
                        var lineParts = contents[i + 1].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                        long ticks;
                        if (long.TryParse(lineParts[0], out ticks))
                        {
                            var info = new FeatureRegInfo
                            {
                                LastChecked = new DateTime(ticks)
                            };

                            if (lineParts.Length > 1 && long.TryParse(lineParts[1], out ticks))
                            {
                                info.ExpirationDate = new DateTime(ticks);
                            }

                            SetUpdateRecord(feat, info);
                        }
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

                var dateLine = pair.Value.LastChecked.Ticks.ToString(CultureInfo.InvariantCulture) + "|" +
                               pair.Value.ExpirationDate.Ticks.ToString(CultureInfo.InvariantCulture);

                lines.Add(dateLine);
            }

            var licenseFile = Filename;
            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(licenseFile));
            lock (_fileLock)
            {
                _fileSystem.WriteAllLines(licenseFile, lines);
            }
        }
    }

    internal class FeatureRegInfo
    {
        public DateTime ExpirationDate { get; set; }
        public DateTime LastChecked { get; set; }

        public FeatureRegInfo()
        {
            ExpirationDate = DateTime.MinValue;
        }
    }
}
