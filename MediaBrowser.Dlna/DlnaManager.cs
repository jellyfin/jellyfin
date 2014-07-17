using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Dlna.Profiles;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Dlna
{
    public class DlnaManager : IDlnaManager
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public DlnaManager(IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IApplicationPaths appPaths,
            ILogger logger,
            IJsonSerializer jsonSerializer)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _logger = logger;
            _jsonSerializer = jsonSerializer;

            //DumpProfiles();
        }

        public IEnumerable<DeviceProfile> GetProfiles()
        {
            ExtractProfilesIfNeeded();

            var list = GetProfiles(UserProfilesPath, DeviceProfileType.User)
                .OrderBy(i => i.Name)
                .ToList();

            list.AddRange(GetProfiles(SystemProfilesPath, DeviceProfileType.System)
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
                new MediaMonkeyProfile(),
                new Windows81Profile(),
                //new WindowsMediaCenterProfile(),
                new WindowsPhoneProfile(),
                new AndroidProfile(),
                new DefaultProfile()
            };

            foreach (var item in list)
            {
                var path = Path.Combine(_appPaths.ProgramDataPath, _fileSystem.GetValidFilename(item.Name) + ".xml");

                _xmlSerializer.SerializeToFile(item, path);
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
            ExtractProfilesIfNeeded();

            return new DefaultProfile();
        }

        public DeviceProfile GetProfile(DeviceIdentification deviceInfo)
        {
            if (deviceInfo == null)
            {
                throw new ArgumentNullException("deviceInfo");
            }

            var profile = GetProfiles()
                .FirstOrDefault(i => i.Identification != null && IsMatch(deviceInfo, i.Identification));

            if (profile != null)
            {
                _logger.Debug("Found matching device profile: {0}", profile.Name);
            }
            else
            {
                _logger.Debug("No matching device profile found. The default will need to be used.");
                LogUnmatchedProfile(deviceInfo);
            }

            return profile;
        }

        private void LogUnmatchedProfile(DeviceIdentification profile)
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("DeviceDescription:{0}", profile.DeviceDescription ?? string.Empty));
            builder.AppendLine(string.Format("FriendlyName:{0}", profile.FriendlyName ?? string.Empty));
            builder.AppendLine(string.Format("Manufacturer:{0}", profile.Manufacturer ?? string.Empty));
            builder.AppendLine(string.Format("ManufacturerUrl:{0}", profile.ManufacturerUrl ?? string.Empty));
            builder.AppendLine(string.Format("ModelDescription:{0}", profile.ModelDescription ?? string.Empty));
            builder.AppendLine(string.Format("ModelName:{0}", profile.ModelName ?? string.Empty));
            builder.AppendLine(string.Format("ModelNumber:{0}", profile.ModelNumber ?? string.Empty));
            builder.AppendLine(string.Format("ModelUrl:{0}", profile.ModelUrl ?? string.Empty));
            builder.AppendLine(string.Format("SerialNumber:{0}", profile.SerialNumber ?? string.Empty));

            _logger.LogMultiline("No matching device profile found. The default will need to be used.", LogSeverity.Info, builder);
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
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            var profile = GetProfiles().FirstOrDefault(i => i.Identification != null && IsMatch(headers, i.Identification));

            if (profile != null)
            {
                _logger.Debug("Found matching device profile: {0}", profile.Name);
            }
            else
            {
                string userAgent = null;
                headers.TryGetValue("User-Agent", out userAgent);

                var msg = "No matching device profile found. The default will be used. ";
                if (!string.IsNullOrEmpty(userAgent))
                {
                    msg += "User-agent: " + userAgent + ". ";
                }

                _logger.Debug(msg);
            }

            return profile;
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

        private IEnumerable<DeviceProfile> GetProfiles(string path, DeviceProfileType type)
        {
            try
            {
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, ".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(i => ParseProfileXmlFile(i.FullName, type))
                    .Where(i => i != null)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<DeviceProfile>();
            }
        }

        private DeviceProfile ParseProfileXmlFile(string path, DeviceProfileType type)
        {
            try
            {
                var profile = (DeviceProfile)_xmlSerializer.DeserializeFromFile(typeof(DeviceProfile), path);

                profile.Id = path.ToLower().GetMD5().ToString("N");
                profile.ProfileType = type;

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
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            var info = GetProfileInfosInternal().First(i => string.Equals(i.Info.Id, id));

            return ParseProfileXmlFile(info.Path, info.Info.Type);
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

        public void DeleteProfile(string id)
        {
            var info = GetProfileInfosInternal().First(i => string.Equals(id, i.Info.Id));

            if (info.Info.Type == DeviceProfileType.System)
            {
                throw new ArgumentException("System profiles cannot be deleted.");
            }

            File.Delete(info.Path);
        }

        public void CreateProfile(DeviceProfile profile)
        {
            profile = ReserializeProfile(profile);

            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            var newFilename = _fileSystem.GetValidFilename(profile.Name) + ".xml";
            var path = Path.Combine(UserProfilesPath, newFilename);

            _xmlSerializer.SerializeToFile(profile, path);
        }

        public void UpdateProfile(DeviceProfile profile)
        {
            profile = ReserializeProfile(profile);

            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                throw new ArgumentException("Profile is missing Id");
            }
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            var current = GetProfileInfosInternal().First(i => string.Equals(i.Info.Id, profile.Id, StringComparison.OrdinalIgnoreCase));

            var newFilename = _fileSystem.GetValidFilename(profile.Name) + ".xml";
            var path = Path.Combine(UserProfilesPath, newFilename);

            if (!string.Equals(path, current.Path, StringComparison.Ordinal) &&
                current.Info.Type != DeviceProfileType.System)
            {
                File.Delete(current.Path);
            }

            _xmlSerializer.SerializeToFile(profile, path);
        }

        /// <summary>
        /// Recreates the object using serialization, to ensure it's not a subclass.
        /// If it's a subclass it may not serlialize properly to xml (different root element tag name)
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        private DeviceProfile ReserializeProfile(DeviceProfile profile)
        {
            if (profile.GetType() == typeof(DeviceProfile))
            {
                return profile;
            }

            var json = _jsonSerializer.SerializeToString(profile);

            return _jsonSerializer.DeserializeFromString<DeviceProfile>(json);
        }

        class InternalProfileInfo
        {
            internal DeviceProfileInfo Info { get; set; }
            internal string Path { get; set; }
        }

        public string GetServerDescriptionXml(IDictionary<string, string> headers, string serverUuId)
        {
            var profile = GetProfile(headers) ??
                          GetDefaultProfile();

            return new DescriptionXmlBuilder(profile, serverUuId, "").GetXml();
        }

        public DlnaIconResponse GetIcon(string filename)
        {
            var format = filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? ImageFormat.Png
                : ImageFormat.Jpg;

            return new DlnaIconResponse
            {
                Format = format,
                Stream = GetType().Assembly.GetManifestResourceStream("MediaBrowser.Dlna.Images." + filename.ToLower())
            };
        }
    }
}