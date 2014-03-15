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
        /// <param name="friendlyName">Name of the friendly.</param>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="modelNumber">The model number.</param>
        /// <param name="manufacturer">The manufacturer.</param>
        /// <returns>DlnaProfile.</</returns>
        DeviceProfile GetProfile(string friendlyName, string modelName, string modelNumber, string manufacturer);
    }
}
