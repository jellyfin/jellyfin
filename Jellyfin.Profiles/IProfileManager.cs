using System;
using System.Collections.Generic;
using System.Net;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Profiles
{
    /// <summary>
    /// Defines the <see cref="IProfileManager"/>.
    /// </summary>
    public interface IProfileManager
    {
        /// <summary>
        /// Gets the device profiles.
        /// </summary>
        IReadOnlyCollection<DeviceProfile> Profiles { get; }

        /// <summary>
        /// Reloads all disk based profile templates.
        /// </summary>
        void ReloadUserTemplates();

        /// <summary>
        /// Adds a profile to the list.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/> instance to add.</param>
        /// <param name="saveToDisk">True if the profile should be saved to disk.</param>
        void AddProfile(DeviceProfile profile, bool saveToDisk = true);

        /// <summary>
        /// Returns a new instance of the <see cref="DeviceProfile"/> using the disk based default profile.
        /// If that is missing, the predefined default profile is used.
        /// </summary>
        /// <returns>A <see cref="DeviceProfile"/> set to the default profile.</returns>
        DeviceProfile DefaultProfile();

        /// <summary>
        /// Updates the profile <paramref name="profileId"/> with <paramref name="newProfile"/>.
        /// </summary>
        /// <param name="profileId">The current <see cref="DeviceProfile"/> instance.</param>
        /// <param name="newProfile">The new <see cref="DeviceProfile"/> instance.</param>
        /// <returns>Boolean value representing the success of the operation.</returns>
        bool UpdateProfile(Guid profileId, DeviceProfile newProfile);

        /// <summary>
        /// Deletes the profile with the id of <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Boolean value representing the success of the operation.</returns>
        bool DeleteProfile(Guid id);

        /// <summary>
        /// Retrieves a profile with the id of <paramref name="id"/>.
        /// If the setting MonitorProfiles is set, the value returned is read directly from the disk. (pre 10.8 compatibility).
        /// </summary>
        /// <param name="id">The profile identifier to locate.</param>
        /// <param name="attemptToRefreshFromDisk">Implies that, if enabled, this profile should be refreshed.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile? GetProfile(Guid id, bool attemptToRefreshFromDisk);

        /// <summary>
        /// Retrieves a profile based upon the information in <paramref name="deviceInfo"/>.
        /// </summary>
        /// <param name="deviceInfo">The device information in a <see cref="DeviceDetails"/>.</param>
        /// <param name="address">Optional: The <see cref="IPAddress"/> of the device.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetProfile(DeviceDetails deviceInfo, IPAddress? address = null);

        /// <summary>
        /// Retrieves or creates a profile based upon the information in <paramref name="deviceInfo"/>.
        /// </summary>
        /// <param name="deviceInfo">The device information in a <see cref="DeviceDetails"/>.</param>
        /// <param name="address">Optional: The <see cref="IPAddress"/> of the device.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetOrCreateProfile(DeviceDetails deviceInfo, IPAddress? address = null);

        /// <summary>
        /// Gets the profile based on the request headers. If no match is found, the default profile is returned.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> of the connection.</param>
        /// <param name="caps">Optional. The device's <see cref="DeviceProfile"/> to use if not found.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetProfile(IHeaderDictionary headers, IPAddress address, DeviceProfile? caps = null);

        /// <summary>
        /// Gets or creates a profile based the request headers. If no match is found, a custom profile is generated from the default profile.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> of the connection.</param>
        /// <param name="caps">Optional. The device's <see cref="DeviceProfile"/> to use if not found.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetOrCreateProfile(IHeaderDictionary headers, IPAddress address, DeviceProfile? caps = null);

        /// <summary>
        /// Retrieves the list of profiles.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{DeviceProfileInfo}"/> containing the information.</returns>
        IEnumerable<DeviceProfileInfo> GetProfileInfos();

        /// <summary>
        /// Saves a user profile instance to disk, ensuring it's name and path are unique.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/> instance.</param>
        void SaveProfileToDisk(DeviceProfile profile);
    }
}
