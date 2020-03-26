#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.Dlna.Profiles;
using Emby.Dlna.Server;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Emby.Dlna
{
    public class DlnaManager : IDlnaManager
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationHost _appHost;
        private static readonly Assembly _assembly = typeof(DlnaManager).Assembly;

        private readonly Dictionary<string, Tuple<InternalProfileInfo, DeviceProfile>> _profiles = new Dictionary<string, Tuple<InternalProfileInfo, DeviceProfile>>(StringComparer.Ordinal);

        public DlnaManager(
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IApplicationPaths appPaths,
            ILoggerFactory loggerFactory,
            IJsonSerializer jsonSerializer,
            IServerApplicationHost appHost)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _logger = loggerFactory.CreateLogger("Dlna");
            _jsonSerializer = jsonSerializer;
            _appHost = appHost;
        }

        public async Task InitProfilesAsync()
        {
            try
            {
                await ExtractSystemProfilesAsync();
                LoadProfiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting DLNA profiles.");
            }
        }

        private void LoadProfiles()
        {
            var list = GetProfiles(UserProfilesPath, DeviceProfileType.User)
                .OrderBy(i => i.Name)
                .ToList();

            list.AddRange(GetProfiles(SystemProfilesPath, DeviceProfileType.System)
                .OrderBy(i => i.Name));
        }

        public IEnumerable<DeviceProfile> GetProfiles()
        {
            lock (_profiles)
            {
                var list = _profiles.Values.ToList();
                return list
                    .OrderBy(i => i.Item1.Info.Type == DeviceProfileType.User ? 0 : 1)
                    .ThenBy(i => i.Item1.Info.Name)
                    .Select(i => i.Item2)
                    .ToList();
            }

        }

        public DeviceProfile GetDefaultProfile()
        {
            return new DefaultProfile();
        }

        public DeviceProfile GetProfile(DeviceIdentification deviceInfo)
        {
            if (deviceInfo == null)
            {
                throw new ArgumentNullException(nameof(deviceInfo));
            }

            var profile = GetProfiles()
                .FirstOrDefault(i => i.Identification != null && IsMatch(deviceInfo, i.Identification));

            if (profile != null)
            {
                _logger.LogDebug("Found matching device profile: {0}", profile.Name);
            }
            else
            {
                LogUnmatchedProfile(deviceInfo);
            }

            return profile;
        }

        private void LogUnmatchedProfile(DeviceIdentification profile)
        {
            var builder = new StringBuilder();

            builder.AppendLine("No matching device profile found. The default will need to be used.");
            builder.AppendLine(string.Format("DeviceDescription:{0}", profile.DeviceDescription ?? string.Empty));
            builder.AppendLine(string.Format("FriendlyName:{0}", profile.FriendlyName ?? string.Empty));
            builder.AppendLine(string.Format("Manufacturer:{0}", profile.Manufacturer ?? string.Empty));
            builder.AppendLine(string.Format("ManufacturerUrl:{0}", profile.ManufacturerUrl ?? string.Empty));
            builder.AppendLine(string.Format("ModelDescription:{0}", profile.ModelDescription ?? string.Empty));
            builder.AppendLine(string.Format("ModelName:{0}", profile.ModelName ?? string.Empty));
            builder.AppendLine(string.Format("ModelNumber:{0}", profile.ModelNumber ?? string.Empty));
            builder.AppendLine(string.Format("ModelUrl:{0}", profile.ModelUrl ?? string.Empty));
            builder.AppendLine(string.Format("SerialNumber:{0}", profile.SerialNumber ?? string.Empty));

            _logger.LogInformation(builder.ToString());
        }

        private bool IsMatch(DeviceIdentification deviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrEmpty(profileInfo.DeviceDescription))
            {
                if (deviceInfo.DeviceDescription == null || !IsRegexMatch(deviceInfo.DeviceDescription, profileInfo.DeviceDescription))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.FriendlyName))
            {
                if (deviceInfo.FriendlyName == null || !IsRegexMatch(deviceInfo.FriendlyName, profileInfo.FriendlyName))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.Manufacturer))
            {
                if (deviceInfo.Manufacturer == null || !IsRegexMatch(deviceInfo.Manufacturer, profileInfo.Manufacturer))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.ManufacturerUrl))
            {
                if (deviceInfo.ManufacturerUrl == null || !IsRegexMatch(deviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelDescription))
            {
                if (deviceInfo.ModelDescription == null || !IsRegexMatch(deviceInfo.ModelDescription, profileInfo.ModelDescription))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelName))
            {
                if (deviceInfo.ModelName == null || !IsRegexMatch(deviceInfo.ModelName, profileInfo.ModelName))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelNumber))
            {
                if (deviceInfo.ModelNumber == null || !IsRegexMatch(deviceInfo.ModelNumber, profileInfo.ModelNumber))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelUrl))
            {
                if (deviceInfo.ModelUrl == null || !IsRegexMatch(deviceInfo.ModelUrl, profileInfo.ModelUrl))
                    return false;
            }

            if (!string.IsNullOrEmpty(profileInfo.SerialNumber))
            {
                if (deviceInfo.SerialNumber == null || !IsRegexMatch(deviceInfo.SerialNumber, profileInfo.SerialNumber))
                    return false;
            }

            return true;
        }

        private bool IsRegexMatch(string input, string pattern)
        {
            try
            {
                return Regex.IsMatch(input, pattern);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error evaluating regex pattern {Pattern}", pattern);
                return false;
            }
        }

        public DeviceProfile GetProfile(IHeaderDictionary headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            var profile = GetProfiles().FirstOrDefault(i => i.Identification != null && IsMatch(headers, i.Identification));

            if (profile != null)
            {
                _logger.LogDebug("Found matching device profile: {0}", profile.Name);
            }
            else
            {
                var headerString = string.Join(", ", headers.Select(i => string.Format("{0}={1}", i.Key, i.Value)).ToArray());
                _logger.LogDebug("No matching device profile found. {0}", headerString);
            }

            return profile;
        }

        private bool IsMatch(IHeaderDictionary headers, DeviceIdentification profileInfo)
        {
            return profileInfo.Headers.Any(i => IsMatch(headers, i));
        }

        private bool IsMatch(IHeaderDictionary headers, HttpHeaderInfo header)
        {
            // Handle invalid user setup
            if (string.IsNullOrEmpty(header.Name))
            {
                return false;
            }

            if (headers.TryGetValue(header.Name, out StringValues value))
            {
                switch (header.Match)
                {
                    case HeaderMatchType.Equals:
                        return string.Equals(value, header.Value, StringComparison.OrdinalIgnoreCase);
                    case HeaderMatchType.Substring:
                        var isMatch = value.ToString().IndexOf(header.Value, StringComparison.OrdinalIgnoreCase) != -1;
                        //_logger.LogDebug("IsMatch-Substring value: {0} testValue: {1} isMatch: {2}", value, header.Value, isMatch);
                        return isMatch;
                    case HeaderMatchType.Regex:
                        return Regex.IsMatch(value, header.Value, RegexOptions.IgnoreCase);
                    default:
                        throw new ArgumentException("Unrecognized HeaderMatchType");
                }
            }

            return false;
        }

        private string UserProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user");

        private string SystemProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system");

        private IEnumerable<DeviceProfile> GetProfiles(string path, DeviceProfileType type)
        {
            try
            {
                var xmlFies = _fileSystem.GetFilePaths(path)
                    .Where(i => string.Equals(Path.GetExtension(i), ".xml", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return xmlFies
                    .Select(i => ParseProfileFile(i, type))
                    .Where(i => i != null)
                    .ToList();
            }
            catch (IOException)
            {
                return new List<DeviceProfile>();
            }
        }

        private DeviceProfile ParseProfileFile(string path, DeviceProfileType type)
        {
            lock (_profiles)
            {
                if (_profiles.TryGetValue(path, out Tuple<InternalProfileInfo, DeviceProfile> profileTuple))
                {
                    return profileTuple.Item2;
                }

                try
                {
                    DeviceProfile profile;

                    var tempProfile = (DeviceProfile)_xmlSerializer.DeserializeFromFile(typeof(DeviceProfile), path);

                    profile = ReserializeProfile(tempProfile);

                    profile.Id = path.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);

                    _profiles[path] = new Tuple<InternalProfileInfo, DeviceProfile>(GetInternalProfileInfo(_fileSystem.GetFileInfo(path), type), profile);

                    return profile;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing profile file: {Path}", path);

                    return null;
                }
            }
        }

        public DeviceProfile GetProfile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            var info = GetProfileInfosInternal().First(i => string.Equals(i.Info.Id, id, StringComparison.OrdinalIgnoreCase));

            return ParseProfileFile(info.Path, info.Info.Type);
        }

        private IEnumerable<InternalProfileInfo> GetProfileInfosInternal()
        {
            lock (_profiles)
            {
                var list = _profiles.Values.ToList();
                return list
                    .Select(i => i.Item1)
                    .OrderBy(i => i.Info.Type == DeviceProfileType.User ? 0 : 1)
                    .ThenBy(i => i.Info.Name);
            }
        }

        public IEnumerable<DeviceProfileInfo> GetProfileInfos()
        {
            return GetProfileInfosInternal().Select(i => i.Info);
        }

        private InternalProfileInfo GetInternalProfileInfo(FileSystemMetadata file, DeviceProfileType type)
        {
            return new InternalProfileInfo
            {
                Path = file.FullName,

                Info = new DeviceProfileInfo
                {
                    Id = file.FullName.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture),
                    Name = _fileSystem.GetFileNameWithoutExtension(file),
                    Type = type
                }
            };
        }

        private async Task ExtractSystemProfilesAsync()
        {
            var namespaceName = GetType().Namespace + ".Profiles.Xml.";

            var systemProfilesPath = SystemProfilesPath;

            foreach (var name in _assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(namespaceName))
                {
                    continue;
                }

                var filename = Path.GetFileName(name).Substring(namespaceName.Length);

                var path = Path.Combine(systemProfilesPath, filename);

                using (var stream = _assembly.GetManifestResourceStream(name))
                {
                    var fileInfo = _fileSystem.GetFileInfo(path);

                    if (!fileInfo.Exists || fileInfo.Length != stream.Length)
                    {
                        Directory.CreateDirectory(systemProfilesPath);

                        using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }

            // Not necessary, but just to make it easy to find
            Directory.CreateDirectory(UserProfilesPath);
        }

        public void DeleteProfile(string id)
        {
            var info = GetProfileInfosInternal().First(i => string.Equals(id, i.Info.Id, StringComparison.OrdinalIgnoreCase));

            if (info.Info.Type == DeviceProfileType.System)
            {
                throw new ArgumentException("System profiles cannot be deleted.");
            }

            _fileSystem.DeleteFile(info.Path);

            lock (_profiles)
            {
                _profiles.Remove(info.Path);
            }
        }

        public void CreateProfile(DeviceProfile profile)
        {
            profile = ReserializeProfile(profile);

            if (string.IsNullOrEmpty(profile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            var newFilename = _fileSystem.GetValidFilename(profile.Name) + ".xml";
            var path = Path.Combine(UserProfilesPath, newFilename);

            SaveProfile(profile, path, DeviceProfileType.User);
        }

        public void UpdateProfile(DeviceProfile profile)
        {
            profile = ReserializeProfile(profile);

            if (string.IsNullOrEmpty(profile.Id))
            {
                throw new ArgumentException("Profile is missing Id");
            }
            if (string.IsNullOrEmpty(profile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            var current = GetProfileInfosInternal().First(i => string.Equals(i.Info.Id, profile.Id, StringComparison.OrdinalIgnoreCase));

            var newFilename = _fileSystem.GetValidFilename(profile.Name) + ".xml";
            var path = Path.Combine(UserProfilesPath, newFilename);

            if (!string.Equals(path, current.Path, StringComparison.Ordinal) &&
                current.Info.Type != DeviceProfileType.System)
            {
                _fileSystem.DeleteFile(current.Path);
            }

            SaveProfile(profile, path, DeviceProfileType.User);
        }

        private void SaveProfile(DeviceProfile profile, string path, DeviceProfileType type)
        {
            lock (_profiles)
            {
                _profiles[path] = new Tuple<InternalProfileInfo, DeviceProfile>(GetInternalProfileInfo(_fileSystem.GetFileInfo(path), type), profile);
            }
            SerializeToXml(profile, path);
        }

        internal void SerializeToXml(DeviceProfile profile, string path)
        {
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

        public string GetServerDescriptionXml(IHeaderDictionary headers, string serverUuId, string serverAddress)
        {
            var profile = GetProfile(headers) ??
                          GetDefaultProfile();

            var serverId = _appHost.SystemId;

            return new DescriptionXmlBuilder(profile, serverUuId, serverAddress, _appHost.FriendlyName, serverId).GetXml();
        }

        public ImageStream GetIcon(string filename)
        {
            var format = filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? ImageFormat.Png
                : ImageFormat.Jpg;

            var resource = GetType().Namespace + ".Images." + filename.ToLowerInvariant();

            return new ImageStream
            {
                Format = format,
                Stream = _assembly.GetManifestResourceStream(resource)
            };
        }
    }
    /*
    class DlnaProfileEntryPoint : IServerEntryPoint
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IXmlSerializer _xmlSerializer;

        public DlnaProfileEntryPoint(IApplicationPaths appPaths, IFileSystem fileSystem, IXmlSerializer xmlSerializer)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _xmlSerializer = xmlSerializer;
        }

        public void Run()
        {
            DumpProfiles();
        }

        private void DumpProfiles()
        {
            DeviceProfile[] list = new []
            {
                new SamsungSmartTvProfile(),
                new XboxOneProfile(),
                new SonyPs3Profile(),
                new SonyPs4Profile(),
                new SonyBravia2010Profile(),
                new SonyBravia2011Profile(),
                new SonyBravia2012Profile(),
                new SonyBravia2013Profile(),
                new SonyBravia2014Profile(),
                new SonyBlurayPlayer2013(),
                new SonyBlurayPlayer2014(),
                new SonyBlurayPlayer2015(),
                new SonyBlurayPlayer2016(),
                new SonyBlurayPlayerProfile(),
                new PanasonicVieraProfile(),
                new WdtvLiveProfile(),
                new DenonAvrProfile(),
                new LinksysDMA2100Profile(),
                new LgTvProfile(),
                new Foobar2000Profile(),
                new SharpSmartTvProfile(),
                new MediaMonkeyProfile(),
                //new Windows81Profile(),
                //new WindowsMediaCenterProfile(),
                //new WindowsPhoneProfile(),
                new DirectTvProfile(),
                new DishHopperJoeyProfile(),
                new DefaultProfile(),
                new PopcornHourProfile(),
                new MarantzProfile()
            };

            foreach (var item in list)
            {
                var path = Path.Combine(_appPaths.ProgramDataPath, _fileSystem.GetValidFilename(item.Name) + ".xml");

                _xmlSerializer.SerializeToFile(item, path);
            }
        }

        public void Dispose()
        {
        }
    }*/
}
