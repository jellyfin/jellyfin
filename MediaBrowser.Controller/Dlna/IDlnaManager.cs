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
    }
}
