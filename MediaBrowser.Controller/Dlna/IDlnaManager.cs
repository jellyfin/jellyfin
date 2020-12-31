#nullable enable

using System.Collections.Generic;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Dlna
{
    /// <summary>
    /// Defines the <see cref="IDlnaManager" />.
    /// </summary>
    public interface IDlnaManager
    {
        /// <summary>
        /// Gets the profile infos.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{DeviceProfileInfo}"/>.</returns>
        IEnumerable<DeviceProfileInfo> GetProfileInfos();

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>A <see cref="DeviceProfile"/>.</returns>
        DeviceProfile? GetProfile(IHeaderDictionary headers);

        /// <summary>
        /// Gets the default profile.
        /// </summary>
        /// <returns>A <see cref="DeviceProfile"/>.</returns>
        DeviceProfile GetDefaultProfile();

        /// <summary>
        /// Creates the profile.
        /// </summary>
        /// <param name="profile">The profile.</param>
        void CreateProfile(DeviceProfile profile);

        /// <summary>
        /// Updates the profile.
        /// </summary>
        /// <param name="profile">The profile.</param>
        void UpdateProfile(DeviceProfile profile);

        /// <summary>
        /// Deletes the profile.
        /// </summary>
        /// <param name="id">The identifier.</param>
        void DeleteProfile(string id);

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>A <see cref="DeviceProfile"/>.</returns>
        DeviceProfile? GetProfile(string id);

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        /// <returns>A <see cref="DeviceProfile"/> or null if not found.</returns>
        DeviceProfile? GetProfile(DeviceIdentification deviceInfo);

        /// <summary>
        /// Gets the icon.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>DlnaIconResponse.</returns>
        ImageStream GetIcon(string filename);
    }
}
