using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.DeviceProfiles
{
    /// <summary>
    /// Defines the <see cref="DeviceProfileManager"/>.
    /// </summary>
    public class DeviceProfileManager : IDeviceProfileManager
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly List<DeviceProfile> _profiles = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProfileManager"/> class.
        /// </summary>
        /// <param name="xmlSerializer">An instance of the <see cref="IXmlSerializer"/>.</param>
        /// <param name="appPaths">An instance of the <see cref="IApplicationPaths"/>.</param>
        /// <param name="logger">An instance of the <see cref="ILogger{ProfileManager}"/>.</param>
        /// <param name="fileSystem">An instance of the <see cref="IFileSystem"/>.</param>
        /// <param name="config">An instance of the <see cref="IServerConfigurationManager"/>.</param>
        public DeviceProfileManager(
            IXmlSerializer xmlSerializer,
            IApplicationPaths appPaths,
            ILogger<DeviceProfileManager> logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config)
        {
            _xmlSerializer = xmlSerializer;
            _appPaths = appPaths;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;

            // Only load the user templates, enabling device templates to be overridden.
            LoadUserTemplates();
        }

        /// <summary>
        /// Gets the device profiles.
        /// </summary>
        public IReadOnlyCollection<DeviceProfile> Profiles => _profiles;

        private string UserProfilesPath => Path.Combine(_appPaths.ConfigurationDirectoryPath, "profiles");

        /// <inheritdoc/>
        public void ReloadUserTemplates()
        {
            lock (_profiles)
            {
                _profiles.RemoveAll(p => p.ProfileType == DeviceProfileType.UserTemplate);
            }

            LoadUserTemplates();
        }

        /// <inheritdoc/>
        public DeviceProfile GetOrCreateProfile(DeviceIdentification deviceIdentification, IPAddress? address = null)
        {
            if (deviceIdentification == null)
            {
                throw new ArgumentNullException(nameof(deviceIdentification));
            }

            var addr = address?.ToString() ?? deviceIdentification.Address;

            var profile = GetProfileInternal(deviceIdentification, addr);

            if (profile == null)
            {
                _logger.LogDebug("No profile found. Using the default profile.");
                return CreateProfileFrom(DefaultProfile(), deviceIdentification.FriendlyName ?? deviceIdentification.ModelName, addr);
            }

            if (profile.ProfileType != DeviceProfileType.Profile && profile.Address == null)
            {
                // Create a clone for this ip address.
                profile = CreateProfileFrom(profile, null, addr);
            }

            _logger.LogDebug("Found matching device profile: {Name}", profile.Name);
            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile GetOverrideProfile(DeviceProfile deviceProfile, IPAddress? address = null)
        {
            if (deviceProfile == null)
            {
                throw new ArgumentNullException(nameof(deviceProfile));
            }

            if (deviceProfile.Identification == null)
            {
                deviceProfile.Identification = new ()
                {
                    ProfileName = deviceProfile.Name
                };
            }

            var profile = GetProfileInternal(deviceProfile.Identification, address?.ToString());
            if (profile == null)
            {
                // No override profile.
                return deviceProfile;
            }

            _logger.LogDebug("Using user profile {Name} instead of device profile {OriginalName}", profile.Name, deviceProfile.Name);
            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile GetProfile(IHeaderDictionary headers, IPAddress address, DeviceProfile? deviceCapabilities)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var addrString = address.ToString();
            DeviceProfile? profile = null;
            int bestMatch = -1;
            int bestMatchType = 0;
            foreach (var item in Profiles)
            {
                var matchRating = 0;

                if (item.Address != null)
                {
                    if (!string.Equals(item.Address, addrString, StringComparison.Ordinal))
                    {
                        // ignore where ip address exists, but doesn't match.
                        continue;
                    }

                    matchRating = ProfileComparison.IpMatch;
                }

                if (item.Identification != null)
                {
                    matchRating += item.Identification.Matches(headers, addrString);
                }

                if (matchRating < bestMatch || matchRating == ProfileComparison.NoMatch)
                {
                    // worse match.
                    continue;
                }

                if (matchRating > bestMatch)
                {
                    // Better match.
                    bestMatch = matchRating;
                    bestMatchType = (int)item.ProfileType;
                    profile = item;
                    continue;
                }

                if ((int)item.ProfileType < bestMatchType)
                {
                    // Same match rating. User Template takes president over a System Template.
                    profile = item;
                    bestMatchType = (int)item.ProfileType;
                }
            }

            if (profile == null)
            {
                _logger.LogDebug(
                        "No matching device profile found for {Header} at {Address}",
                        string.Join(", ", headers.Select(i => string.Format(CultureInfo.InvariantCulture, "{0}={1}", i.Key, i.Value))),
                        address);
                return deviceCapabilities ?? DefaultProfile();
            }

            _logger.LogDebug("Found a match with device profile for {Name}", profile.Name);

            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile GetOrCreateProfile(IHeaderDictionary headers, IPAddress address)
        {
            var profile = GetProfile(headers, address, null);
            if (profile.ProfileType != DeviceProfileType.Profile || string.IsNullOrEmpty(profile.Address))
            {
                var newProfile = new DeviceProfile(profile)
                {
                    Address = address.ToString()
                };

                AddProfile(newProfile);
                _logger.LogDebug("No profile found. Using the default profile.");
                return newProfile;
            }

            _logger.LogDebug("Found matching device profile: {Name}", profile.Name);
            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile? GetProfile(Guid id, bool noProfileRefresh)
        {
            var profile = Profiles.FirstOrDefault(i => i.Id.Equals(id));

            if (profile == null || (profile.ProfileType != DeviceProfileType.UserTemplate) || noProfileRefresh)
            {
                return profile;
            }

            // When editing user profiles, they are re-read from disk.

            // replace the memory based profile with the disk based one.
            lock (_profiles)
            {
                var diskProfile = ParseProfileFile(profile.Path!, profile.ProfileType);
                _profiles.Remove(profile);

                if (diskProfile != null)
                {
                    // if it still exists, re-add the disk version.
                    _profiles.Add(diskProfile);
                }

                return diskProfile;
            }
        }

        /// <inheritdoc/>
        public bool DeleteProfile(Guid id)
        {
            var i = Profiles.FirstOrDefault(p => p.Id.Equals(id));
            if (i == null)
            {
                // Profile no longer exists, so ignore.
                return true;
            }

            switch (i.ProfileType)
            {
                case DeviceProfileType.SystemTemplate:
                    throw new ArgumentException("System profiles cannot be deleted.");

                case DeviceProfileType.UserTemplate:
                    try
                    {
                        // This can only be null in test units.
                        if (i.Path != null)
                        {
                            File.Delete(i.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to delete file {Path}", i.Path);
                        throw new ArgumentException("Error whilst attempting to delete profile.");
                    }

                    break;

                case DeviceProfileType.Profile:
                    throw new ArgumentException("Unable to delete temporary profile.");
            }

            lock (_profiles)
            {
                _profiles.Remove(i);
            }

            return true;
        }

        /// <inheritdoc/>
        public DeviceProfile DefaultProfile()
        {
            // It is technically possible to have two default profiles in memory, the system defined, and the user defined, so order the response.
            return Profiles.Where(p => p.Id.Equals(Guid.Empty))
                .OrderBy(p => p.ProfileType)
                .First();
        }

        /// <inheritdoc/>
        public bool UpdateProfile(Guid profileId, DeviceProfile newProfile)
        {
            if (newProfile == null)
            {
                throw new ArgumentNullException(nameof(newProfile));
            }

            var currentProfile = GetProfile(profileId, true);
            if ((currentProfile == null) || (currentProfile.ProfileType == DeviceProfileType.Profile))
            {
                // Can only update a template.
                return false;
            }

            if (string.IsNullOrEmpty(newProfile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            newProfile.ProfileType = DeviceProfileType.UserTemplate;

            if (currentProfile.ProfileType != DeviceProfileType.UserTemplate)
            {
                AddProfile(newProfile);
                SaveProfileToDisk(newProfile);
                return true;
            }

            // if the profile name has changed.
            if (!string.Equals(currentProfile.Name, newProfile.Name, StringComparison.Ordinal) && !string.IsNullOrEmpty(currentProfile.Path))
            {
                _fileSystem.DeleteFile(currentProfile.Path);
            }

            // update the version we have in memory.
            lock (_profiles)
            {
                _profiles.Remove(currentProfile);

                newProfile.Id = currentProfile.Id;
                newProfile.Path = currentProfile.Path;

                _profiles.Add(newProfile);
            }

            SaveProfileToDisk(newProfile);
            return true;
        }

        /// <inheritdoc/>
        public IEnumerable<DeviceProfileInfo> GetProfileInfos()
        {
            return Profiles
                .Where(p => p.ProfileType != DeviceProfileType.Profile)
                .OrderBy(p => (int)p.ProfileType)
                .Select(p => new DeviceProfileInfo(p.Id.ToString("N", CultureInfo.InvariantCulture), string.Empty, p.ProfileType));
        }

        /// <inheritdoc/>
        public string GetUniqueProfileName(string name)
        {
            while (Profiles.Any(p => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var result = Regex.Match(name, @"\d+$").Value;
                if (string.IsNullOrEmpty(result))
                {
                    name += '1';
                    continue;
                }

                name = name[0..^result.Length]
                    + (int.Parse(result, NumberStyles.None, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture);
            }

            return name;
        }

        /// <inheritdoc/>
        public void AddProfile(DeviceProfile profile, bool saveToDisk = true)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            lock (_profiles)
            {
                if (_profiles.Any(p => p.Id.Equals(profile.Id)))
                {
                    throw new ArgumentException("Id not unique");
                }

                _profiles.Add(profile);
            }

            if (profile.ProfileType == DeviceProfileType.UserTemplate && saveToDisk)
            {
                SaveProfileToDisk(profile);
            }
        }

        private DeviceProfile? GetProfileInternal(DeviceIdentification deviceIdentification, string? address)
        {
            var ipAddress = address?.ToString();

            DeviceProfile? profile = null;
            int bestMatch = -1;
            int bestMatchType = 0;

            foreach (var item in Profiles)
            {
                if (!string.IsNullOrEmpty(item.Address) && string.Equals(item.Address, ipAddress, StringComparison.Ordinal))
                {
                    profile = item;
                    break;
                }

                var matchRating = 0;
                if (item.Identification != null)
                {
                    matchRating += deviceIdentification.Matches(item.Identification);
                }

                if (matchRating < bestMatch || matchRating == ProfileComparison.NoMatch)
                {
                    continue;
                }

                if (matchRating > bestMatch)
                {
                    // Better match.
                    bestMatch = matchRating;
                    bestMatchType = (int)item.ProfileType;
                    profile = item;
                    continue;
                }

                if ((int)item.ProfileType < bestMatchType)
                {
                    // Same match, but this one is more specific. (UserTemplate takes president over a SystemTemplate)
                    profile = item;
                    bestMatchType = (int)item.ProfileType;
                }
            }

            return profile;
        }

        /// <summary>
        /// Loads any user profiles from disk.
        /// </summary>
        private void LoadUserTemplates()
        {
            var userProfilesPath = UserProfilesPath;

            if (!Directory.Exists(userProfilesPath))
            {
                try
                {
                    Directory.CreateDirectory(userProfilesPath);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogError(ex, "Unable to create user device profile path {Path}", userProfilesPath);
                    AddProfile(DeviceProfile.DefaultProfile());
                    return;
                }
            }

            _profiles.AddRange(GetProfiles(userProfilesPath, DeviceProfileType.UserTemplate));

            // If the default profile does not exist on disk, then re-add the default profile.
            if (!_profiles.Any(p => p.Id.Equals(Guid.Empty)))
            {
                AddProfile(DeviceProfile.DefaultProfile(), true);
            }
        }

        /// <summary>
        /// Loads all profiles from a folder.
        /// </summary>
        /// <param name="path">The folder.</param>
        /// <param name="type">The <see cref="DeviceProfileType"/> to assign each profile.</param>
        /// <returns>An <see cref="IEnumerable{DeviceProfile}"/>.</returns>
        private IEnumerable<DeviceProfile> GetProfiles(string path, DeviceProfileType type)
        {
            try
            {
                return (IEnumerable<DeviceProfile>)_fileSystem.GetFilePaths(path)
                    .Where(i => string.Equals(Path.GetExtension(i), ".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(i => ParseProfileFile(i, type))
                    .Where(i => i != null)
                    .ToList();
            }
            catch (IOException)
            {
                return new List<DeviceProfile>();
            }
        }

        /// <summary>
        /// Parses a disk profile.
        /// </summary>
        /// <param name="path">The profile to load.</param>
        /// <param name="type">The profile type to assign.</param>
        /// <returns>A <see cref="DeviceProfile"/> representing the profile or null if an error occurred.</returns>
        private DeviceProfile? ParseProfileFile(string path, DeviceProfileType type)
        {
            if (!File.Exists(path))
            {
                _logger.LogDebug("Profile no longer exists : {Path}", path);
                return null;
            }

            try
            {
                var profile = (DeviceProfile)_xmlSerializer.DeserializeFromFile(typeof(DeviceProfile), path);
                if (profile == null)
                {
                    _logger.LogError("Error parsing profile file: {Path}", path);
                    return null;
                }

                profile.Path = path;
                profile.ProfileType = type;
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

        /// <summary>
        /// Saves a user profile instance to disk, ensuring it's name and path are unique.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/> instance.</param>
        public void SaveProfileToDisk(DeviceProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (profile.ProfileType != DeviceProfileType.Profile)
            {
                if (profile.Path == null)
                {
                    string newFilename = _fileSystem.GetValidFilename(GetUniqueProfileName(profile.Name));
                    var basePath = Path.Combine(UserProfilesPath, newFilename);
                    profile.Path = basePath + ".xml";
                }

                _xmlSerializer.SerializeToFile(profile, profile.Path);
            }
        }

        /// <summary>
        /// Builds a new profile based on the information provided.
        /// </summary>
        /// <param name="template">The source <see cref="DeviceProfile"/> to use as a foundation.</param>
        /// <param name="profileName">Optional: The profile name.</param>
        /// <param name="address">Optional: IP address to assign the profile.</param>
        /// <returns>The default <see cref="DeviceProfile"/>.</returns>
        private DeviceProfile CreateProfileFrom(DeviceProfile template, string? profileName, string? address)
        {
            // Get the default user profile.
            var profile = new DeviceProfile(template, profileName)
            {
                Address = address
            };

            AddProfile(profile);

            // AutoCreateProfiles enables new device profiles to be saved in the user folder, so that they are easier to edit.
            if (_config.Configuration.SaveUnknownDeviceProfilesToDisk)
            {
                try
                {
                    profile.ProfileType = DeviceProfileType.UserTemplate;
                    SaveProfileToDisk(profile);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogError(ex, "Error saving default profile for {Name}.", profile.Name);
                }
            }

            return profile;
        }
    }
}
