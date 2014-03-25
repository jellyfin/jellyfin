using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public interface IDlnaManager
    {
        /// <summary>
        /// Gets the dlna profiles.
        /// </summary>
        /// <returns>IEnumerable{DlnaProfile}.</returns>
        IEnumerable<DeviceProfile> GetProfiles();

        /// <summary>
        /// Gets the default profile.
        /// </summary>
        /// <returns>DlnaProfile.</returns>
        DeviceProfile GetDefaultProfile();

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetProfile(IDictionary<string,string> headers);

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetProfile(DeviceIdentification deviceInfo);
    }
}
