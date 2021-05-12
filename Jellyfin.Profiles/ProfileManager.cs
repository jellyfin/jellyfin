using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Profiles
{
    /// <summary>
    /// Defines the <see cref="ProfileManager"/>.
    /// </summary>
    public class ProfileManager : IProfileManager
    {
        private const int NoMatch = 0;
        private const int IpMatch = 1000;

        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly List<DeviceProfile> _profiles = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileManager"/> class.
        /// </summary>
        /// <param name="xmlSerializer">An instance of the <see cref="IXmlSerializer"/>.</param>
        /// <param name="appPaths">An instance of the <see cref="IApplicationPaths"/>.</param>
        /// <param name="logger">An instance of the <see cref="ILogger{ProfileManager}"/>.</param>
        /// <param name="fileSystem">An instance of the <see cref="IFileSystem"/>.</param>
        /// <param name="config">An instance of the <see cref="IServerConfigurationManager"/>.</param>
        public ProfileManager(
            IXmlSerializer xmlSerializer,
            IApplicationPaths appPaths,
            ILogger<ProfileManager> logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config)
        {
            _xmlSerializer = xmlSerializer;
            _appPaths = appPaths;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;

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
        public DeviceProfile GetOrCreateProfile(DeviceDetails deviceInfo, IPAddress? address = null)
        {
            var profile = GetProfile(deviceInfo, address);

            if (profile.Id.Equals(Guid.Empty) || profile.ProfileType != DeviceProfileType.Profile)
            {
                profile = CreateProfileFrom(profile, deviceInfo, address);
                AddProfile(profile);
            }

            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile GetProfile(DeviceDetails deviceInfo, IPAddress? address = null)
        {
            if (deviceInfo == null)
            {
                throw new ArgumentNullException(nameof(deviceInfo));
            }

            // Embed IP address into the deviceInfo instance, keeping the original to restore later.
            var origIp = deviceInfo.Address;
            deviceInfo.Address = address?.ToString() ?? deviceInfo.Address;

            DeviceProfile? profile = null;
            int bestMatch = -1;
            int bestMatchType = 0;
            bool hasAddress = !string.IsNullOrEmpty(deviceInfo.Address);
            foreach (var item in Profiles)
            {
                var matchRating = (hasAddress && string.Equals(item.Address, deviceInfo.Address, StringComparison.Ordinal)) ? IpMatch : 0;

                if (item.Identification != null)
                {
                    matchRating += deviceInfo.Matches(item.Identification);
                }

                if (matchRating < bestMatch || matchRating == NoMatch)
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

            // restore original deviceInfo address.
            deviceInfo.Address = origIp;

            if (profile != null)
            {
                _logger.LogDebug("Found matching device profile: {Name}", profile.Name);
                return profile;
            }

            _logger.LogDebug("No profile found. Using the default profile.");
            return DefaultProfile();
        }

        /// <inheritdoc/>
        public DeviceProfile GetProfile(IHeaderDictionary headers, IPAddress address, DeviceProfile? caps = null)
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

                    matchRating = IpMatch;
                }

                if (item.Identification != null)
                {
                    matchRating += item.Identification.Matches(headers, addrString);
                }

                if (matchRating < bestMatch || matchRating == NoMatch)
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

            if (profile != null)
            {
                _logger.LogDebug("Found a match with device profile for {Name}", profile.FriendlyName);
                return profile;
            }

            _logger.LogDebug(
                "No matching device profile found for {Header} at {Address}",
                string.Join(", ", headers.Select(i => string.Format(CultureInfo.InvariantCulture, "{0}={1}", i.Key, i.Value))),
                address);
            profile = caps ?? DefaultProfile();
            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile GetOrCreateProfile(IHeaderDictionary headers, IPAddress address, DeviceProfile? caps = null)
        {
            var profile = GetProfile(headers, address, caps);
            if (profile.ProfileType != DeviceProfileType.Profile || string.IsNullOrEmpty(profile.Address))
            {
                profile = CloneAsProfile(profile);
                profile.Address = address.ToString();
                AddProfile(profile);
            }

            return profile;
        }

        /// <inheritdoc/>
        public DeviceProfile? GetProfile(Guid id, bool attemptToRefreshFromDisk)
        {
            var profile = Profiles.FirstOrDefault(i => i.Id.Equals(id));

            // The pre-10.8 code auto loaded the profile from disk. This capability can be disabled with the config option MonitorUserTemplates.
            if (profile == null
                || (profile.ProfileType != DeviceProfileType.UserTemplate)
                || !_config.Configuration.MonitorUserTemplates
                || !attemptToRefreshFromDisk)
            {
                return profile;
            }

            // remove the original one from memory.
            lock (_profiles)
            {
                // Attempt to reload and parse the disk based profiles to see if anything has changed.
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
                        File.Delete(i.Path!);
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
            // Try and get the user's default profile loaded from the folder first.
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

            var currentProfile = GetProfile(profileId, false);
            if ((currentProfile == null) || (currentProfile.ProfileType == DeviceProfileType.Profile))
            {
                return false;
            }

            if (string.IsNullOrEmpty(newProfile.Name))
            {
                throw new ArgumentException("Profile is missing Name");
            }

            newProfile.ProfileType = DeviceProfileType.UserTemplate;
            if (currentProfile.ProfileType != DeviceProfileType.SystemTemplate)
            {
                // if the profile name has changed.
                if (!string.Equals(currentProfile.Name, newProfile.Name, StringComparison.Ordinal))
                {
                    // Ensure new name is valid and unique, and delete the old disk profile.
                    EnsureUniqueValidFilename(newProfile);
                    _fileSystem.DeleteFile(currentProfile.Path);
                }

                // update the version we have in memory.
                lock (_profiles)
                {
                    _profiles.Remove(currentProfile);
                    _profiles.Add(newProfile);
                }

                SaveProfileToDisk(newProfile);
                return true;
            }

            // Updated a system profile which is not possible. So create a user based one instead.

            newProfile.Id = Guid.NewGuid();
            EnsureUniqueValidFilename(newProfile);
            AddProfile(newProfile);
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
        public void AddProfile(DeviceProfile profile, bool saveToDisk = true)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            lock (_profiles)
            {
#if DEBUG
                if (_profiles.FirstOrDefault(p => p.Id.Equals(profile.Id)) != null)
                {
                    throw new ArgumentException("Id not unique");
                }
#endif
                _profiles.Add(profile);
            }

            if (profile.ProfileType == DeviceProfileType.UserTemplate && saveToDisk)
            {
                SaveProfileToDisk(profile);
            }
        }

        /// <summary>
        /// Clones a <see cref="DeviceProfile"/> and returning a <see cref="DeviceProfileType.Profile"/>.
        /// </summary>
        /// <param name="source">The <see cref="DeviceProfile"/> to clone.</param>
        /// <returns>The <see cref="DeviceProfile"/> clone.</returns>
        private static DeviceProfile CloneAsProfile(DeviceProfile source)
        {
            return new DeviceProfile()
            {
                Address = source.Address,
                AlbumArtPn = source.AlbumArtPn,
                CodecProfiles = source.CodecProfiles,
                ContainerProfiles = source.ContainerProfiles,
                DirectPlayProfiles = source.DirectPlayProfiles,
                EnableAlbumArtInDidl = source.EnableAlbumArtInDidl,
                EnableMSMediaReceiverRegistrar = source.EnableMSMediaReceiverRegistrar,
                EnableSingleAlbumArtLimit = source.EnableSingleAlbumArtLimit,
                EnableSingleSubtitleLimit = source.EnableSingleSubtitleLimit,
                EncodeContextOnTransmission = source.EncodeContextOnTransmission,
                FriendlyName = source.FriendlyName,
                Id = Guid.NewGuid(),
                Identification = source.Identification,
                IgnoreTranscodeByteRangeRequests = source.IgnoreTranscodeByteRangeRequests,
                Manufacturer = source.Manufacturer,
                ManufacturerUrl = source.ManufacturerUrl,
                MaxAlbumArtHeight = source.MaxAlbumArtHeight,
                MaxAlbumArtWidth = source.MaxAlbumArtWidth,
                MaxIconHeight = source.MaxIconHeight,
                MaxIconWidth = source.MaxIconWidth,
                MaxStaticBitrate = source.MaxStaticBitrate,
                MaxStaticMusicBitrate = source.MaxStaticMusicBitrate,
                MaxStreamingBitrate = source.MaxStreamingBitrate,
                ModelDescription = source.ModelDescription,
                ModelName = source.ModelName,
                ModelNumber = source.ModelNumber,
                ModelUrl = source.ModelUrl,
                MusicStreamingTranscodingBitrate = source.MusicStreamingTranscodingBitrate,
                Name = source.Name ?? source.FriendlyName ?? "Unknown Device",
                Path = null,
                ProfileType = DeviceProfileType.Profile,
                ProtocolInfo = source.ProtocolInfo,
                RequiresPlainFolders = source.RequiresPlainFolders,
                RequiresPlainVideoItems = source.RequiresPlainVideoItems,
                ResponseProfiles = source.ResponseProfiles,
                SerialNumber = source.SerialNumber,
                SonyAggregationFlags = source.SonyAggregationFlags,
                SubtitleProfiles = source.SubtitleProfiles,
                SupportedMediaTypes = source.SupportedMediaTypes,
                TimelineOffsetSeconds = source.TimelineOffsetSeconds,
                TranscodingProfiles = source.TranscodingProfiles,
                UserId = source.UserId,
                XmlRootAttributes = source.XmlRootAttributes,
            };
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

            _profiles.AddRange(GetProfiles(userProfilesPath, DeviceProfileType.SystemTemplate));

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
            if (profile.ProfileType != DeviceProfileType.Profile)
            {
                if (profile.Path == null)
                {
                    // If it hasn't been saved before, then ensure its name and path are unique.
                    EnsureUniqueValidFilename(profile);
                }

                _xmlSerializer.SerializeToFile(profile, profile.Path);
            }
        }

        /// <summary>
        /// Builds a new profile based on the information provided in <paramref name="deviceInfo"/> along with that in the default profile.
        /// </summary>
        /// <param name="source">The source <see cref="DeviceProfile"/> to use as a foundation.</param>
        /// <param name="deviceInfo">The <see cref="DeviceDetails"/>.</param>
        /// <param name="address">Optional: IP address to assign the profile.</param>
        /// <returns>The default <see cref="DeviceProfile"/>.</returns>
        private DeviceProfile CreateProfileFrom(DeviceProfile source, DeviceDetails deviceInfo, IPAddress? address)
        {
            const string Unknown = "Unknown Device";

            // Get the default user profile.
            var profile = CloneAsProfile(source);

            // Assign the properties in deviceIdentification.
            if (deviceInfo is DeviceProfile dp)
            {
                profile.Name = dp.Name ?? deviceInfo.FriendlyName ?? Unknown;
            }
            else
            {
                profile.Name = deviceInfo.FriendlyName ?? Unknown;
            }

            profile.CopyFrom(deviceInfo);
            if (address != null)
            {
                profile.Address = address.ToString();
            }

            // AutoCreateProfiles enables new device profiles to be saved in the user folder, so that they are easier to edit.
            if (_config.Configuration.SaveUnknownDeviceProfilesToDisk)
            {
                try
                {
                    profile.ProfileType = DeviceProfileType.UserTemplate;
                    EnsureUniqueValidFilename(profile);
                    SaveProfileToDisk(profile);
                    return profile;
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

        /// <summary>
        /// Ensures <paramref name="profile"/> has a unique name, and a valid path.
        /// </summary>
        /// <param name="profile">An instance of <see cref="DeviceProfile"/>.</param>
        private void EnsureUniqueValidFilename(DeviceProfile profile)
        {
            profile.Name ??= "New Profile";
            // Ensure only valid characters.
            string newFilename = _fileSystem.GetValidFilename(profile.Name);
            var basePath = Path.Combine(UserProfilesPath, newFilename);
            var path = basePath + ".xml";

            // Ensure the filename (and profile name) are unique.
            var i = 0;
            while (File.Exists(path))
            {
                i++;
                path = basePath + i.ToString(CultureInfo.InvariantCulture) + ".xml";
            }

            profile.Path = path;
            if (i == 0)
            {
                return;
            }

            // Ensure name is unique.
            profile.Name = profile.Name + " " + i.ToString(CultureInfo.InvariantCulture);
        }
    }
}
