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
using Emby.Dlna.Configuration;
using Emby.Dlna.Profiles;
using Emby.Dlna.Server;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
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
        private readonly ILogger<DlnaManager> _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _configurationManager;
        private static readonly Assembly _assembly = typeof(DlnaManager).Assembly;

        private readonly Dictionary<string, Tuple<InternalProfileInfo, DeviceProfile>> _profiles = new Dictionary<string, Tuple<InternalProfileInfo, DeviceProfile>>(StringComparer.Ordinal);

        public DlnaManager(
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IApplicationPaths appPaths,
            ILoggerFactory loggerFactory,
            IJsonSerializer jsonSerializer,
            IServerApplicationHost appHost,
            IServerConfigurationManager configurationManager)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _logger = loggerFactory.CreateLogger<DlnaManager>();
            _jsonSerializer = jsonSerializer;
            _appHost = appHost;
            _configurationManager = configurationManager;
        }

        private string UserProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user");

        private string SystemProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system");

        public async Task InitProfilesAsync()
        {
            try
            {
                await ExtractSystemProfilesAsync().ConfigureAwait(false);
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

        /// <summary>
        /// Copies the properties from a PlayTo PlayToDevice into a DeviceProfile.
        /// </summary>
        /// <param name="playToDeviceInfo">PlayToDeviceInfo.</param>
        private static DeviceProfile PlayToDeviceInfoToDeviceProfile(PlayToDeviceInfo playToDeviceInfo)
        {
            DeviceProfile profile = new DefaultProfile
            {
                Name = playToDeviceInfo.FriendlyName,
                Identification = new DeviceIdentification
                {
                    FriendlyName = playToDeviceInfo.FriendlyName,
                    Manufacturer = playToDeviceInfo.Manufacturer,
                    ModelName = playToDeviceInfo.ModelName
                },
                Manufacturer = playToDeviceInfo.Manufacturer,
                FriendlyName = playToDeviceInfo.FriendlyName,
                ModelNumber = playToDeviceInfo.ModelNumber,
                ModelName = playToDeviceInfo.ModelName,
                ModelUrl = playToDeviceInfo.ModelUrl,
                ModelDescription = playToDeviceInfo.ModelDescription,
                ManufacturerUrl = playToDeviceInfo.ManufacturerUrl,
                SerialNumber = playToDeviceInfo.SerialNumber
            };

            return profile;
        }

        private DeviceProfile AutoCreateProfile(PlayToDeviceInfo playToDeviceInfo)
        {
            if (_configurationManager.GetDlnaConfiguration().AutoCreatePlayToProfiles)
            {
                try
                {
                    var profile = PlayToDeviceInfoToDeviceProfile(playToDeviceInfo);
                    CreateProfile(profile);
                    return profile;
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving default profile for {0}.", playToDeviceInfo.FriendlyName);
                }
            }

            return null;
        }

        public DeviceProfile GetDefaultProfile(PlayToDeviceInfo playToDeviceInfo)
        {
            var profile = PlayToDeviceInfoToDeviceProfile(playToDeviceInfo);
            profile.ProtocolInfo = playToDeviceInfo.Capabilities;
            var supportedMediaTypes = new HashSet<string>();

            foreach (var capability in playToDeviceInfo
                .Capabilities
                .Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var protocolInfo = capability.Split(':');
                var protocolName = "*";
                if (protocolInfo.Length == 4)
                {
                    string org_pn = protocolInfo[3];
                    int index = org_pn.IndexOf("DLNA.ORG_PN", StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        int endIndex = org_pn.IndexOf(',', index + 1);
                        protocolName = org_pn.Substring(index + 12, endIndex);
                    }

                    // Split video/mpg.
                    supportedMediaTypes.Add(protocolInfo[2].Split('/')[0]);
                }
            }

            profile.SupportedMediaTypes = supportedMediaTypes.Aggregate((i, j) => i + ',' + j);

            return profile;
        }

        public DeviceProfile GetProfile(PlayToDeviceInfo playToDeviceInfo)
        {
            if (playToDeviceInfo == null)
            {
                throw new ArgumentNullException(nameof(playToDeviceInfo));
            }

            var profile = GetProfiles()
                .FirstOrDefault(i => i.Identification != null && IsMatch(playToDeviceInfo, i.Identification));

            if (profile != null)
            {
                _logger.LogDebug("Found matching device profile: {0}", profile.Name);
            }
            else
            {
                LogUnmatchedProfile(playToDeviceInfo);
            }

            return profile;
        }

        private void LogUnmatchedProfile(PlayToDeviceInfo profile)
        {
            var builder = new StringBuilder();

            builder.AppendLine("No matching device profile found. The default will need to be used.");
            builder.AppendFormat(CultureInfo.InvariantCulture, "FriendlyName:{0}", profile.FriendlyName ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "Manufacturer:{0}", profile.Manufacturer ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "ManufacturerUrl:{0}", profile.ManufacturerUrl ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "ModelDescription:{0}", profile.ModelDescription ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "ModelName:{0}", profile.ModelName ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "ModelNumber:{0}", profile.ModelNumber ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "ModelUrl:{0}", profile.ModelUrl ?? string.Empty).AppendLine();
            builder.AppendFormat(CultureInfo.InvariantCulture, "SerialNumber:{0}", profile.SerialNumber ?? string.Empty).AppendLine();

            _logger.LogInformation(builder.ToString());
        }

        private bool IsMatch(PlayToDeviceInfo playToDeviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrEmpty(profileInfo.FriendlyName))
            {
                if (playToDeviceInfo.FriendlyName == null || !IsRegexMatch(playToDeviceInfo.FriendlyName, profileInfo.FriendlyName))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.Manufacturer))
            {
                if (playToDeviceInfo.Manufacturer == null || !IsRegexMatch(playToDeviceInfo.Manufacturer, profileInfo.Manufacturer))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ManufacturerUrl))
            {
                if (playToDeviceInfo.ManufacturerUrl == null || !IsRegexMatch(playToDeviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelDescription))
            {
                if (playToDeviceInfo.ModelDescription == null || !IsRegexMatch(playToDeviceInfo.ModelDescription, profileInfo.ModelDescription))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelName))
            {
                if (playToDeviceInfo.ModelName == null || !IsRegexMatch(playToDeviceInfo.ModelName, profileInfo.ModelName))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelNumber))
            {
                if (playToDeviceInfo.ModelNumber == null || !IsRegexMatch(playToDeviceInfo.ModelNumber, profileInfo.ModelNumber))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelUrl))
            {
                if (playToDeviceInfo.ModelUrl == null || !IsRegexMatch(playToDeviceInfo.ModelUrl, profileInfo.ModelUrl))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.SerialNumber))
            {
                if (playToDeviceInfo.SerialNumber == null || !IsRegexMatch(playToDeviceInfo.SerialNumber, profileInfo.SerialNumber))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPropertyMatch(string input, string pattern)
        {
            try
            {
                return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || input.Contains(pattern, StringComparison.OrdinalIgnoreCase);
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
                if (_configurationManager.GetDlnaConfiguration().AutoCreatePlayToProfiles)
                {
                    if (headers.TryGetValue("User-Agent", out StringValues value) && value.Count > 0)
                    {
                        string userAgent = value[0];
                        profile = new DefaultProfile();
                        profile.Name = userAgent;
                        profile.Identification = new DeviceIdentification
                        {
                            FriendlyName = userAgent,
                            Headers = new[]
                            {
                                new HttpHeaderInfo()
                                {
                                    Match = HeaderMatchType.Equals,
                                    Name = "User-Agent",
                                    Value = userAgent
                                }
                            }
                        };

                        profile.Manufacturer = userAgent.Split(' ')[0];
                        profile.FriendlyName = userAgent;
                        profile.ModelName = userAgent;
                        try
                        {
                            CreateProfile(profile);
                            return profile;
                        }
                        catch (IOException ex)
                        {
                            _logger.LogError(ex.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving default profile for {0}.", userAgent);
                        }
                    }
                }

                var headerString = string.Join(", ", headers.Select(i => string.Format(CultureInfo.InvariantCulture, "{0}={1}", i.Key, i.Value)));
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
                        // _logger.LogDebug("IsMatch-Substring value: {0} testValue: {1} isMatch: {2}", value, header.Value, isMatch);
                        return isMatch;
                    case HeaderMatchType.Regex:
                        return Regex.IsMatch(value, header.Value, RegexOptions.IgnoreCase);
                    default:
                        throw new ArgumentException("Unrecognized HeaderMatchType");
                }
            }

            return false;
        }

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
                if (!name.StartsWith(namespaceName, StringComparison.Ordinal))
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
                            await stream.CopyToAsync(fileStream).ConfigureAwait(false);
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
        /// If it's a subclass it may not serlialize properly to xml (different root element tag name).
        /// </summary>
        /// <param name="profile">The device profile.</param>
        /// <returns>The reserialized device profile.</returns>
        private DeviceProfile ReserializeProfile(DeviceProfile profile)
        {
            if (profile.GetType() == typeof(DeviceProfile))
            {
                return profile;
            }

            var json = _jsonSerializer.SerializeToString(profile);

            return _jsonSerializer.DeserializeFromString<DeviceProfile>(json);
        }

        public string GetServerDescriptionXml(IHeaderDictionary headers, string serverUuId, HttpRequest request)
        {
            var profile = GetDefaultProfile();

            var serverId = _appHost.SystemId;

            var serverAddress = _appHost.GetSmartApiUrl(request);

            return new DescriptionXmlBuilder(profile, serverUuId, serverAddress, _appHost.FriendlyName, serverId, _configurationManager.Configuration.ServerName).GetXml();
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

        private class InternalProfileInfo
        {
            internal DeviceProfileInfo Info { get; set; }

            internal string Path { get; set; }
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
                // new Windows81Profile(),
                // new WindowsMediaCenterProfile(),
                // new WindowsPhoneProfile(),
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
