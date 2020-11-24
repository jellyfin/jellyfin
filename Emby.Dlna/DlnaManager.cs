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
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Dlna;
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
    public sealed class DlnaManager : IDlnaManager, IServerEntryPoint
    {
        private static readonly Assembly _assembly = typeof(DlnaManager).Assembly;
        private readonly IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<DlnaManager> _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly Dictionary<string, Tuple<InternalProfileInfo, DeviceProfile>> _profiles = new Dictionary<string, Tuple<InternalProfileInfo, DeviceProfile>>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaManager"/> class.
        /// </summary>
        /// <param name="xmlSerializer">The xmlSerializer<see cref="IXmlSerializer"/>.</param>
        /// <param name="fileSystem">The fileSystem<see cref="IFileSystem"/>.</param>
        /// <param name="appPaths">The appPaths<see cref="IApplicationPaths"/>.</param>
        /// <param name="loggerFactory">The loggerFactory<see cref="ILoggerFactory"/>.</param>
        /// <param name="jsonSerializer">The jsonSerializer<see cref="IJsonSerializer"/>.</param>
        public DlnaManager(
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IApplicationPaths appPaths,
            ILoggerFactory loggerFactory,
            IJsonSerializer jsonSerializer)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _logger = loggerFactory.CreateLogger<DlnaManager>();
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the User Profile Path.
        /// </summary>
        private string UserProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "user");

        /// <summary>
        /// Gets the System Profile Path.
        /// </summary>
        private string SystemProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "dlna", "system");

        /// <summary>
        /// Main function ran automatically at startup.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task RunAsync()
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

        /// <summary>
        /// Retrieves the default profile.
        /// </summary>
        /// <returns>The default profile as a <see cref="DeviceProfile"/>.</returns>
        public DeviceProfile GetDefaultProfile()
        {
            return new DefaultProfile();
        }

        /// <summary>
        /// Gets the profile for the device <paramref name="deviceInfo"/>.
        /// </summary>
        /// <param name="deviceInfo">A <see cref="DeviceIdentification"/>.</param>
        /// <returns>A <see cref="DeviceProfile"/> or null if not matched.</returns>
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
                _logger.LogInformation(LogUnmatchedProfile(deviceInfo));
            }

            return profile;
        }

        /// <summary>
        /// Returns the profile for <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Id to search for.</param>
        /// <returns>A <see cref="DeviceProfile"/>.</returns>
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
        /// Gets all the Profiles Information.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{DeviceProfileInfo}"/>.</returns>
        public IEnumerable<DeviceProfileInfo> GetProfileInfos()
        {
            return GetProfileInfosInternal().Select(i => i.Info);
        }

        /// <summary>
        /// Returns a profile that matches the values in <paramref name="headers"/>.
        /// </summary>
        /// <param name="headers">A <see cref="IHeaderDictionary"/> instance containing device information.</param>
        /// <returns>A <see cref="DeviceProfile"/> instance.</returns>
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
        /// The Disposer.
        /// </summary>
        public void Dispose()
        {
            // Do nothing.
        }

        /// <summary>
        /// Deletes a Profile.
        /// </summary>
        /// <param name="id">The id of the profile to delete.</param>
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

        /// <summary>
        /// Creates a Profile.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
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

        /// <summary>
        /// Updates a Profile.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        public void UpdateProfile(DeviceProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

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

        /// <inheritdoc/>
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

            if (!headers.TryGetValue(header.Name, out StringValues value))
            {
                return false;
            }

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

        private static string LogUnmatchedProfile(DeviceIdentification profile)
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

            return builder.ToString();
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

        /// <summary>
        /// Saves a Profile.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        /// <param name="path">The destination path.</param>
        /// <param name="type">The profile <see cref="DeviceProfileType"/>.</param>
        private void SaveProfile(DeviceProfile profile, string path, DeviceProfileType type)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            lock (_profiles)
            {
                _profiles[path] = new Tuple<InternalProfileInfo, DeviceProfile>(GetInternalProfileInfo(_fileSystem.GetFileInfo(path), type), profile);
            }

            SerializeToXml(profile, path);
        }

        private IEnumerable<DeviceProfile?> GetProfiles(string path, DeviceProfileType type)
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

        private DeviceProfile? ParseProfileFile(string path, DeviceProfileType type)
        {
            lock (_profiles)
            {
                if (_profiles.TryGetValue(path, out Tuple<InternalProfileInfo, DeviceProfile>? profileTuple))
                {
                    return profileTuple!.Item2;
                }

                try
                {
                    var tempProfile = (DeviceProfile)_xmlSerializer.DeserializeFromFile(typeof(DeviceProfile), path);

                    DeviceProfile profile = ReserializeProfile(tempProfile);

                    profile.Id = path.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);

                    _profiles[path] = new Tuple<InternalProfileInfo, DeviceProfile>(GetInternalProfileInfo(_fileSystem.GetFileInfo(path), type), profile);

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

        private InternalProfileInfo GetInternalProfileInfo(FileSystemMetadata file, DeviceProfileType type)
        {
            return new InternalProfileInfo(
                file.FullName,
                new DeviceProfileInfo
                {
                    Id = file.FullName.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture),
                    Name = _fileSystem.GetFileNameWithoutExtension(file),
                    Type = type
                });
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

                var path = Path.Join(
                    systemProfilesPath,
                    Path.GetFileName(name.AsSpan())[namespaceName.Length..]);

                await using var stream = _assembly.GetManifestResourceStream(name);
                if (stream == null)
                {
                    throw new ResourceNotFoundException($"Resource {name} missing from manifest:");
                }

                var fileInfo = _fileSystem.GetFileInfo(path);

                if (fileInfo.Exists && fileInfo.Length == stream.Length)
                {
                    continue;
                }

                Directory.CreateDirectory(systemProfilesPath);

                await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            // Not necessary, but just to make it easy to find
            Directory.CreateDirectory(UserProfilesPath);
        }

        private void SerializeToXml(DeviceProfile profile, string path)
        {
            _xmlSerializer.SerializeToFile(profile, path);
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

            var json = _jsonSerializer.SerializeToString(profile);

            return _jsonSerializer.DeserializeFromString<DeviceProfile>(json);
        }

        private void LoadProfiles()
        {
            var list = GetProfiles(UserProfilesPath, DeviceProfileType.User)
                .OrderBy(i => i?.Name)
                .ToList();

            list.AddRange(GetProfiles(SystemProfilesPath, DeviceProfileType.System)
                .OrderBy(i => i?.Name));
        }

        /// <summary>
        /// Defines the <see cref="InternalProfileInfo" />.
        /// </summary>
        private class InternalProfileInfo
        {
            public InternalProfileInfo(string path, DeviceProfileInfo info)
            {
                Info = info;
                Path = path;
            }

            /// <summary>
            /// Gets the Info.
            /// </summary>
            internal DeviceProfileInfo Info { get; }

            /// <summary>
            /// Gets the Path.
            /// </summary>
            internal string Path { get; }
        }
    }
}
