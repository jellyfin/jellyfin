using System;
using System.Collections.Generic;
using System.Net;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.DeviceProfiles
{
    /// <summary>
    /// Defines the <see cref="IDeviceProfileManager"/>.
    /// </summary>
    public interface IDeviceProfileManager
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
        /// <param name="saveToDisk">Optional: Saves the profile to disk.</param>
        /// <returns>Boolean value representing the success of the operation.</returns>
        bool UpdateProfile(Guid profileId, DeviceProfile newProfile, bool saveToDisk = false);

        /// <summary>
        /// Deletes the profile with the id of <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Boolean value representing the success of the operation.</returns>
        bool DeleteProfile(Guid id);

        /// <summary>
        /// Retrieves a profile with the id of <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The profile identifier to locate.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile? GetProfile(Guid id);

        /// <summary>
        /// Retrieves an override profile based upon the information in <paramref name="deviceProfile"/>.
        /// </summary>
        /// <param name="deviceProfile">The device information in a <see cref="DeviceIdentification"/>.</param>
        /// <param name="address">Optional: The <see cref="IPAddress"/> of the device.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetOverrideProfile(DeviceProfile deviceProfile, IPAddress? address = null);

        /// <summary>
        /// Retrieves or creates a profile based upon the information in <paramref name="deviceIdentification"/>.
        /// </summary>
        /// <param name="deviceIdentification">The device information in a <see cref="DeviceIdentification"/>.</param>
        /// <param name="address">Optional: The <see cref="IPAddress"/> of the device.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetOrCreateProfile(DeviceIdentification deviceIdentification, IPAddress? address = null);

        /// <summary>
        /// Gets the profile based on the request headers. If no match is found, the default profile is returned.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> of the connection.</param>
        /// <param name="deviceCapabilities">The device's <see cref="DeviceProfile"/> to use if not found.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetProfile(IHeaderDictionary headers, IPAddress address, DeviceProfile? deviceCapabilities);

        /// <summary>
        /// Gets or creates a profile based the request headers. If no match is found, a custom profile is generated from the default profile.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> of the connection.</param>
        /// <returns>The <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetOrCreateProfile(IHeaderDictionary headers, IPAddress address);

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

        /// <summary>
        /// Returns a unique name.
        /// </summary>
        /// <remarks>
        /// Attempt to increment the last number. eg. Profile1 becomes Profile2, Profile9 becomes Profile10, Profile becomes Profile1.
        /// </remarks>
        /// <param name="name">Original name.</param>
        /// <returns>A pretty unique name.</returns>
        string GetUniqueProfileName(string name);
    }
}
