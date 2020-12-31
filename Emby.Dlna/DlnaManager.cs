#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.Dlna.Profiles;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Json;
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
    /// <summary>
    /// Defines the <see cref="DlnaManager" />.
    /// </summary>
    public class DlnaManager : IDlnaManager
    {
        private static readonly Assembly _assembly = typeof(DlnaManager).Assembly;
        private readonly IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<DlnaManager> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly Dictionary<string, (InternalProfileInfo profileInfo, DeviceProfile deviceProfile)> _profiles;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.GetOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaManager"/> class.
        /// </summary>
        /// <param name="xmlSerializer">The <see cref="IXmlSerializer"/>.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
        /// <param name="appPaths">The <see cref="IApplicationPaths"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
        public DlnaManager(
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IApplicationPaths appPaths,
            ILoggerFactory loggerFactory,
            IServerApplicationHost appHost)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _logger = loggerFactory.CreateLogger<DlnaManager>();
            _appHost = appHost;
            _profiles = new (StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets the user profiles path.
        /// </summary>
        private string UserProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user");

        /// <summary>
        /// Gets the system profiles path.
        /// </summary>
        private string SystemProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system");

        /// <summary>
        /// Initialises the profiles.
        /// </summary>
        /// <returns>The initialisation <see cref="Task"/>.</returns>
        public async Task InitProfilesAsync()
        {
            try
            {
                await ExtractSystemProfilesAsync().ConfigureAwait(false);
                LoadProfiles();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Error extracting DLNA profiles.");
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void CreateProfile(DeviceProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile = ReserializeProfile(profile);

            if (string.IsNullOrEmpty(profile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            var newFilename = _fileSystem.GetValidFilename(profile.Name) + ".xml";
            var path = Path.Combine(UserProfilesPath, newFilename);

            SaveProfile(profile, path, DeviceProfileType.User);
        }

        /// <inheritdoc/>
        public void UpdateProfile(DeviceProfile? profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile = ReserializeProfile(profile);

            if (string.IsNullOrEmpty(profile?.Id))
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

        /// <summary>
        /// Gets all the profiles.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{DeviceProfile}"/> containing all the profiles.</returns>
        public IEnumerable<DeviceProfile> GetProfiles()
        {
            lock (_profiles)
            {
                var list = _profiles.Values.ToList();
                return list
                    .OrderBy(i => i.profileInfo.Info.Type == DeviceProfileType.User ? 0 : 1)
                    .ThenBy(i => i.profileInfo.Info.Name)
                    .Select(i => i.deviceProfile)
                    .ToList();
            }
        }

        /// <inheritdoc/>
        public DeviceProfile GetDefaultProfile()
        {
            return new DefaultProfile();
        }

        /// <inheritdoc/>
        public DeviceProfile? GetProfile(DeviceIdentification deviceInfo)
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

        /// <inheritdoc/>
        public DeviceProfile? GetProfile(IHeaderDictionary headers)
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
                var headerString = string.Join(", ", headers.Select(i => string.Format(CultureInfo.InvariantCulture, "{0}={1}", i.Key, i.Value)));
                _logger.LogDebug("No matching device profile found. {0}", headerString);
            }

            return profile;
        }

        /// <summary>
        /// Gets a Profile.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The <see cref="DeviceProfile"/> if found, otherwise null.</returns>
        public DeviceProfile? GetProfile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            var info = GetProfileInfosInternal().First(i => string.Equals(i.Info.Id, id, StringComparison.OrdinalIgnoreCase));

            return ParseProfileFile(info.Path, info.Info.Type);
        }

        /// <summary>
        /// Gets the Profile Info records.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{DeviceProfileInfo}"/>.</returns>
        public IEnumerable<DeviceProfileInfo> GetProfileInfos()
        {
            return GetProfileInfosInternal().Select(i => i.Info);
        }

        /// <summary>
        /// Gets the Icon.
        /// </summary>
        /// <param name="filename">The filename of the icon.</param>
        /// <returns>The <see cref="ImageStream"/>.</returns>
        public ImageStream GetIcon(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

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

        /// <summary>
        /// Serializes a profile to Xml.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        /// <param name="path">The path.</param>
        internal void SerializeToXml(DeviceProfile profile, string path)
        {
            _xmlSerializer.SerializeToFile(profile, path);
        }

        private static bool IsMatch(IHeaderDictionary headers, DeviceIdentification profileInfo)
        {
            return profileInfo.Headers.Any(i => IsMatch(headers, i));
        }

        private static bool IsMatch(IHeaderDictionary headers, HttpHeaderInfo header)
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
                var xmlFiles = _fileSystem.GetFilePaths(path)
                    .Where(i => string.Equals(Path.GetExtension(i), ".xml", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return xmlFiles
                    .Select(i => ParseProfileFile(i, type))
                    .Where(i => i != null)
                    .ToList()!;
            }
            catch (IOException)
            {
                return new List<DeviceProfile>();
            }
        }

        private DeviceProfile? ParseProfileFile(string path, DeviceProfileType type)
        {
            lock (_profiles)
            {
                if (_profiles.TryGetValue(path, out var profileTuple))
                {
                    return profileTuple.deviceProfile;
                }

                try
                {
                    DeviceProfile profile;

                    var tempProfile = (DeviceProfile)_xmlSerializer.DeserializeFromFile(typeof(DeviceProfile), path);

                    profile = ReserializeProfile(tempProfile);

                    profile.Id = path.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);

                    _profiles[path] = (GetInternalProfileInfo(_fileSystem.GetFileInfo(path), type), profile);

                    return profile;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogError(ex, "Error parsing profile file: {Path}", path);

                    return null;
                }
            }
        }

        private void LogUnmatchedProfile(DeviceIdentification profile)
        {
            var builder = new StringBuilder();

            builder.AppendLine("No matching device profile found. The default will need to be used.");
            builder.Append("FriendlyName:").AppendLine(profile.FriendlyName);
            builder.Append("Manufacturer:").AppendLine(profile.Manufacturer);
            builder.Append("ManufacturerUrl:").AppendLine(profile.ManufacturerUrl);
            builder.Append("ModelDescription:").AppendLine(profile.ModelDescription);
            builder.Append("ModelName:").AppendLine(profile.ModelName);
            builder.Append("ModelNumber:").AppendLine(profile.ModelNumber);
            builder.Append("ModelUrl:").AppendLine(profile.ModelUrl);
            builder.Append("SerialNumber:").AppendLine(profile.SerialNumber);

            _logger.LogInformation(builder.ToString());
        }

        private bool IsMatch(DeviceIdentification deviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrEmpty(profileInfo.FriendlyName))
            {
                if (deviceInfo.FriendlyName == null || !IsRegexOrSubstringMatch(deviceInfo.FriendlyName, profileInfo.FriendlyName))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.Manufacturer))
            {
                if (deviceInfo.Manufacturer == null || !IsRegexOrSubstringMatch(deviceInfo.Manufacturer, profileInfo.Manufacturer))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ManufacturerUrl))
            {
                if (deviceInfo.ManufacturerUrl == null || !IsRegexOrSubstringMatch(deviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelDescription))
            {
                if (deviceInfo.ModelDescription == null || !IsRegexOrSubstringMatch(deviceInfo.ModelDescription, profileInfo.ModelDescription))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelName))
            {
                if (deviceInfo.ModelName == null || !IsRegexOrSubstringMatch(deviceInfo.ModelName, profileInfo.ModelName))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelNumber))
            {
                if (deviceInfo.ModelNumber == null || !IsRegexOrSubstringMatch(deviceInfo.ModelNumber, profileInfo.ModelNumber))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.ModelUrl))
            {
                if (deviceInfo.ModelUrl == null || !IsRegexOrSubstringMatch(deviceInfo.ModelUrl, profileInfo.ModelUrl))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(profileInfo.SerialNumber))
            {
                if (deviceInfo.SerialNumber == null || !IsRegexOrSubstringMatch(deviceInfo.SerialNumber, profileInfo.SerialNumber))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsRegexOrSubstringMatch(string input, string pattern)
        {
            try
            {
                return input.Contains(pattern, StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error evaluating regex pattern {Pattern}", pattern);
                return false;
            }
        }

        private IEnumerable<InternalProfileInfo> GetProfileInfosInternal()
        {
            lock (_profiles)
            {
                var list = _profiles.Values.ToList();
                return list
                    .Select(i => i.profileInfo)
                    .OrderBy(i => i.Info.Type == DeviceProfileType.User ? 0 : 1)
                    .ThenBy(i => i.Info.Name);
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

        private InternalProfileInfo GetInternalProfileInfo(FileSystemMetadata file, DeviceProfileType type)
        {
            return new InternalProfileInfo(
                file.FullName,
                new DeviceProfileInfo(
                    file.FullName.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture),
                    _fileSystem.GetFileNameWithoutExtension(file),
                    type));
        }

        /// <summary>
        /// The ExtractSystemProfilesAsync.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
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

                var path = Path.Join(
                    systemProfilesPath,
                    Path.GetFileName(name.AsSpan())[namespaceName.Length..]);

                using var stream = _assembly.GetManifestResourceStream(name);
                if (stream == null)
                {
                    throw new ResourceNotFoundException(name);
                }

                var fileInfo = _fileSystem.GetFileInfo(path);

                if (!fileInfo.Exists || fileInfo.Length != stream.Length)
                {
                    Directory.CreateDirectory(systemProfilesPath);

                    using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }

            // Not necessary, but just to make it easy to find
            Directory.CreateDirectory(UserProfilesPath);
        }

        /// <summary>
        /// The SaveProfile.
        /// </summary>
        /// <param name="profile">The profile<see cref="DeviceProfile"/>.</param>
        /// <param name="path">The path<see cref="string"/>.</param>
        /// <param name="type">The type<see cref="DeviceProfileType"/>.</param>
        private void SaveProfile(DeviceProfile profile, string path, DeviceProfileType type)
        {
            lock (_profiles)
            {
                _profiles[path] = (GetInternalProfileInfo(_fileSystem.GetFileInfo(path), type), profile);
            }

            SerializeToXml(profile, path);
        }

        /// <summary>
        /// Recreates the object using serialization, to ensure it's not a subclass.
        /// If it's a subclass it may not serialize properly to xml (different root element tag name).
        /// </summary>
        /// <param name="profile">The device profile.</param>
        /// <returns>The re-serialized device profile.</returns>
        private DeviceProfile ReserializeProfile(DeviceProfile profile)
        {
            if (profile.GetType() == typeof(DeviceProfile))
            {
                return profile;
            }

            var json = JsonSerializer.Serialize(profile, _jsonOptions);

            return JsonSerializer.Deserialize<DeviceProfile>(json, _jsonOptions)
                ?? throw new JsonException("Unable to deserialize profile id :" + profile.Id);
        }

        /// <summary>
        /// Defines the <see cref="InternalProfileInfo" />.
        /// </summary>
        private class InternalProfileInfo
        {
            internal InternalProfileInfo(string path, DeviceProfileInfo info)
            {
                Path = path;
                Info = info;
            }

            /// <summary>
            /// Gets or sets the Info.
            /// </summary>
            internal DeviceProfileInfo Info { get; set; }

            /// <summary>
            /// Gets or sets the Path.
            /// </summary>
            internal string Path { get; set; }
        }
    }
}
