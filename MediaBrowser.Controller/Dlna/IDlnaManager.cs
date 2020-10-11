#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Gets the default profile.
        /// </summary>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetDefaultProfile();

        /// <summary>
        /// Gets the default profile based on the capabilities provided.
        /// </summary>
        /// <param name="playToDeviceInfo">The PlayTo device information record.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetDefaultProfile(PlayToDeviceInfo playToDeviceInfo);

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

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="playToDeviceInfo">The device information.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetProfile(PlayToDeviceInfo playToDeviceInfo);

        /// <summary>
        /// Gets the server description XML.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="serverUuId">The server uu identifier.</param>
        /// <param name="request">The http request instance.</param>
        /// <returns>System.String.</returns>
        string GetServerDescriptionXml(IHeaderDictionary headers, string serverUuId, HttpRequest request);

        /// <summary>
        /// Gets the icon.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>DlnaIconResponse.</returns>
        ImageStream GetIcon(string filename);

        /// <summary>
        /// Extracts all profiles, and loads them up.
        /// </summary>
        Task InitProfilesAsync();
    }
}
