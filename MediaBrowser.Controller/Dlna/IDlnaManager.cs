using MediaBrowser.Model.Dlna;
using System.Collections.Generic;

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
        DeviceProfile GetProfile(IDictionary<string,string> headers);

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
        
        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetProfile(DeviceIdentification deviceInfo);

        /// <summary>
        /// Gets the server description XML.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="serverUuId">The server uu identifier.</param>
        /// <returns>System.String.</returns>
        string GetServerDescriptionXml(IDictionary<string, string> headers, string serverUuId);

        /// <summary>
        /// Gets the content directory XML.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>System.String.</returns>
        string GetContentDirectoryXml(IDictionary<string, string> headers);

        /// <summary>
        /// Processes the control request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>ControlResponse.</returns>
        ControlResponse ProcessControlRequest(ControlRequest request);
    }
}
