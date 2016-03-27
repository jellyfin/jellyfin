using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Common.Implementations.Security
{
    internal class MBLicenseFile
    {
        private readonly IApplicationPaths _appPaths;

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

        public MBLicenseFile(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;

            Load();
        }

        private void SetUpdateRecord(Guid key, DateTime value)
        {
            _updateRecords.AddOrUpdate(key, value, (k, v) => value);
        }

        public void AddRegCheck(string featureId)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                var key = new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(featureId)));
                var value = DateTime.UtcNow;

                SetUpdateRecord(key, value);
                Save();
            }

        }

        public void RemoveRegCheck(string featureId)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                var key = new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(featureId)));
                DateTime val;

                _updateRecords.TryRemove(key, out val);

                Save();
            }

        }

        public DateTime LastChecked(string featureId)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                DateTime last;
                _updateRecords.TryGetValue(new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(featureId))), out last);

                // guard agains people just putting a large number in the file
                return last < DateTime.UtcNow ? last : DateTime.MinValue;  
            }
        }

        private void Load()
        {
            string[] contents = null;
            var licenseFile = Filename;
            lock (_fileLock)
            {
                try
                {
					contents = File.ReadAllLines(licenseFile);
                }
                catch (DirectoryNotFoundException)
                {
                    File.Create(licenseFile).Close();
                }
                catch (FileNotFoundException)
                {
					File.Create(licenseFile).Close();
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
                    var feat = Guid.Parse(contents[i]);

                    SetUpdateRecord(feat, new DateTime(Convert.ToInt64(contents[i + 1])));
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
			Directory.CreateDirectory(Path.GetDirectoryName(licenseFile));
			lock (_fileLock) File.WriteAllLines(licenseFile, lines);
        }
    }
}
