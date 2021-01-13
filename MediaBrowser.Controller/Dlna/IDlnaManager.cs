#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Dlna
{
    public interface IDlnaManager
    {
        /// <summary>
        /// Gets the profile infos.
        /// </summary>
        /// <returns>IEnumerable{DeviceProfileInfo}.</returns>
        IEnumerable<DeviceProfileInfo> GetProfileInfos();

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetProfile(IHeaderDictionary headers);

        /// <summary>
        /// Gets all the profile.
        /// </summary>
        /// <returns><see cref="IEnumerable{DeviceProfile}"/>.</returns>
        IEnumerable<DeviceProfile> GetProfiles();

        /// <summary>
        /// Gets the default profile.
        /// </summary>
        /// <returns>DeviceProfile.</returns>
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
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetProfile(string id);
    }
}
