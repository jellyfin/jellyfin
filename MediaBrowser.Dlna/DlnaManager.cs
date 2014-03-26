using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Dlna.Profiles;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Dlna
{
    public class DlnaManager : IDlnaManager
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public DlnaManager(IXmlSerializer xmlSerializer, IFileSystem fileSystem, IApplicationPaths appPaths, ILogger logger)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _logger = logger;

            //DumpProfiles();
        }

        public IEnumerable<DeviceProfile> GetProfiles()
        {
            ExtractProfilesIfNeeded();

            var list = GetProfiles(UserProfilesPath)
                .OrderBy(i => i.Name)
                .ToList();

            list.AddRange(GetProfiles(SystemProfilesPath)
                .OrderBy(i => i.Name));

            return list;
        }

        private void DumpProfiles()
        {
            var list = new List<DeviceProfile>
            {
                new SamsungSmartTvProfile(),
                new Xbox360Profile(),
                new XboxOneProfile(),
                new SonyPs3Profile(),
                new SonyBravia2010Profile(),
                new SonyBravia2011Profile(),
                new SonyBravia2012Profile(),
                new SonyBravia2013Profile(),
                new SonyBlurayPlayer2013Profile(),
                new SonyBlurayPlayerProfile(),
                new PanasonicVieraProfile(),
                new WdtvLiveProfile(),
                new DenonAvrProfile(),
                new LinksysDMA2100Profile(),
                new LgTvProfile(),
                new Foobar2000Profile(),
                new DefaultProfile()
            };

            foreach (var item in list)
            {
                _xmlSerializer.SerializeToFile(item, "d:\\" + _fileSystem.GetValidFilename(item.Name) + ".xml");
            }
        }

        private bool _extracted;
        private readonly object _syncLock = new object();
        private void ExtractProfilesIfNeeded()
        {
            if (!_extracted)
            {
                lock (_syncLock)
                {
                    if (!_extracted)
                    {
                        try
                        {
                            ExtractSystemProfiles();
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error extracting DLNA profiles.", ex);
                        }

                        _extracted = true;
                    }

                }
            }
        }

        public DeviceProfile GetDefaultProfile()
        {
            return new DefaultProfile();
        }

        public DeviceProfile GetProfile(DeviceIdentification deviceInfo)
        {
            var profile = GetProfiles().FirstOrDefault(i => IsMatch(deviceInfo, i.Identification)) ??
                GetDefaultProfile();

            _logger.Debug("Found matching device profile: {0}", profile.Name);

            return profile;
        }

        private bool IsMatch(DeviceIdentification deviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrWhiteSpace(profileInfo.DeviceDescription))
            {
                if (deviceInfo.DeviceDescription == null || !Regex.IsMatch(deviceInfo.DeviceDescription, profileInfo.DeviceDescription))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.FriendlyName))
            {
                if (deviceInfo.FriendlyName == null || !Regex.IsMatch(deviceInfo.FriendlyName, profileInfo.FriendlyName))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.Manufacturer))
            {
                if (deviceInfo.Manufacturer == null || !Regex.IsMatch(deviceInfo.Manufacturer, profileInfo.Manufacturer))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ManufacturerUrl))
            {
                if (deviceInfo.ManufacturerUrl == null || !Regex.IsMatch(deviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelDescription))
            {
                if (deviceInfo.ModelDescription == null || !Regex.IsMatch(deviceInfo.ModelDescription, profileInfo.ModelDescription))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelName))
            {
                if (deviceInfo.ModelName == null || !Regex.IsMatch(deviceInfo.ModelName, profileInfo.ModelName))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelNumber))
            {
                if (deviceInfo.ModelNumber == null || !Regex.IsMatch(deviceInfo.ModelNumber, profileInfo.ModelNumber))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelUrl))
            {
                if (deviceInfo.ModelUrl == null || !Regex.IsMatch(deviceInfo.ModelUrl, profileInfo.ModelUrl))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.SerialNumber))
            {
                if (deviceInfo.SerialNumber == null || !Regex.IsMatch(deviceInfo.SerialNumber, profileInfo.SerialNumber))
                    return false;
            }

            return true;
        }

        public DeviceProfile GetProfile(IDictionary<string, string> headers)
        {
            return GetProfiles().FirstOrDefault(i => IsMatch(headers, i.Identification)) ??
                GetDefaultProfile();
        }

        private bool IsMatch(IDictionary<string, string> headers, DeviceIdentification profileInfo)
        {
            return profileInfo.Headers.Any(i => IsMatch(headers, i));
        }

        private bool IsMatch(IDictionary<string, string> headers, HttpHeaderInfo header)
        {
            string value;

            if (headers.TryGetValue(header.Name, out value))
            {
                switch (header.Match)
                {
                    case HeaderMatchType.Equals:
                        return string.Equals(value, header.Value, StringComparison.OrdinalIgnoreCase);
                    case HeaderMatchType.Substring:
                        return value.IndexOf(header.Value, StringComparison.OrdinalIgnoreCase) != -1;
                    case HeaderMatchType.Regex:
                        return Regex.IsMatch(value, header.Value, RegexOptions.IgnoreCase);
                    default:
                        throw new ArgumentException("Unrecognized HeaderMatchType");
                }
            }

            return false;
        }

        private string UserProfilesPath
        {
            get
            {
                return Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user");
            }
        }

        private string SystemProfilesPath
        {
            get
            {
                return Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system");
            }
        }

        private IEnumerable<DeviceProfile> GetProfiles(string path)
        {
            try
            {
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, ".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(i => ParseProfileXmlFile(i.FullName))
                    .Where(i => i != null)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<DeviceProfile>();
            }
        }

        private DeviceProfile ParseProfileXmlFile(string path)
        {
            try
            {
                var profile = (DeviceProfile)_xmlSerializer.DeserializeFromFile(typeof(DeviceProfile), path);

                profile.Id = path.ToLower().GetMD5().ToString("N");

                return profile;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error parsing profile xml: {0}", ex, path);

                return null;
            }
        }

        public DeviceProfile GetProfile(string id)
        {
            var info = GetProfileInfosInternal().First(i => string.Equals(i.Info.Id, id));

            return ParseProfileXmlFile(info.Path);
        }

        private IEnumerable<InternalProfileInfo> GetProfileInfosInternal()
        {
            ExtractProfilesIfNeeded();

            return GetProfileInfos(UserProfilesPath, DeviceProfileType.User)
                .Concat(GetProfileInfos(SystemProfilesPath, DeviceProfileType.System))
                .OrderBy(i => i.Info.Type == DeviceProfileType.User ? 0 : 1)
                .ThenBy(i => i.Info.Name);
        }

        public IEnumerable<DeviceProfileInfo> GetProfileInfos()
        {
            return GetProfileInfosInternal().Select(i => i.Info);
        }

        private IEnumerable<InternalProfileInfo> GetProfileInfos(string path, DeviceProfileType type)
        {
            try
            {
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, ".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(i => new InternalProfileInfo
                    {
                        Path = i.FullName,

                        Info = new DeviceProfileInfo
                        {
                            Id = i.FullName.ToLower().GetMD5().ToString("N"),
                            Name = Path.GetFileNameWithoutExtension(i.FullName),
                            Type = type
                        }
                    })
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<InternalProfileInfo>();
            }
        }

        private void ExtractSystemProfiles()
        {
            var assembly = GetType().Assembly;
            var namespaceName = GetType().Namespace + ".Profiles.Xml.";

            var systemProfilesPath = SystemProfilesPath;

            foreach (var name in assembly.GetManifestResourceNames()
                .Where(i => i.StartsWith(namespaceName))
                .ToList())
            {
                var filename = Path.GetFileName(name).Substring(namespaceName.Length);

                var path = Path.Combine(systemProfilesPath, filename);

                using (var stream = assembly.GetManifestResourceStream(name))
                {
                    var fileInfo = new FileInfo(path);

                    if (!fileInfo.Exists || fileInfo.Length != stream.Length)
                    {
                        Directory.CreateDirectory(systemProfilesPath);

                        using (var fileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }

            // Not necessary, but just to make it easy to find
            Directory.CreateDirectory(UserProfilesPath);
        }

        class InternalProfileInfo
        {
            internal DeviceProfileInfo Info { get; set; }
            internal string Path { get; set; }
        }
    }
}