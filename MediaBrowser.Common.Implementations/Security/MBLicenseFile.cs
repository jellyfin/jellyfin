using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
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
                    UpdateRecords.Clear();
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

        public string LegacyKey { get; set; }
        private Dictionary<Guid, DateTime> UpdateRecords { get; set; }
        private readonly object _lck = new object();
        private string _regKey;

        public MBLicenseFile(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;

            UpdateRecords = new Dictionary<Guid, DateTime>();
            Load();
        }

        public void AddRegCheck(string featureId)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                UpdateRecords[new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(featureId)))] = DateTime.UtcNow;
                Save();
            }

        }

        public void RemoveRegCheck(string featureId)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                UpdateRecords.Remove(new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(featureId))));
                Save();
            }

        }

        public DateTime LastChecked(string featureId)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                DateTime last;
                lock(_lck) UpdateRecords.TryGetValue(new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(featureId))), out last);
                return last < DateTime.UtcNow ? last : DateTime.MinValue;  // guard agains people just putting a large number in the file
            }
        }

        private void Load()
        {
            string[] contents = null;
            var licenseFile = Filename;
            lock (_lck)
            {
                try
                {
                    contents = File.ReadAllLines(licenseFile);
                }
                catch (FileNotFoundException)
                {
                    (File.Create(licenseFile)).Close();
                }
            }
            if (contents != null && contents.Length > 0)
            {
                //first line is reg key
                RegKey = contents[0];
                //next is legacy key
                if (contents.Length > 1) LegacyKey = contents[1];
                //the rest of the lines should be pairs of features and timestamps
                for (var i = 2; i < contents.Length; i = i + 2)
                {
                    var feat = Guid.Parse(contents[i]);
                    UpdateRecords[feat] = new DateTime(Convert.ToInt64(contents[i + 1]));
                }
            }
        }

        public void Save()
        {
            //build our array
            var lines = new List<string> {RegKey, LegacyKey};
            foreach (var pair in UpdateRecords)
            {
                lines.Add(pair.Key.ToString());
                lines.Add(pair.Value.Ticks.ToString());
            }

            var licenseFile = Filename;
            Directory.CreateDirectory(Path.GetDirectoryName(licenseFile));
            lock (_lck) File.WriteAllLines(licenseFile, lines);
        }
    }
}
