using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public interface IDlnaManager
    {
        /// <summary>
        /// Gets the dlna profiles.
        /// </summary>
        /// <returns>IEnumerable{DlnaProfile}.</returns>
        IEnumerable<DlnaProfile> GetProfiles();

        /// <summary>
        /// Gets the default profile.
        /// </summary>
        /// <returns>DlnaProfile.</returns>
        DlnaProfile GetDefaultProfile();

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="friendlyName">Name of the friendly.</param>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="modelNumber">The model number.</param>
        /// <returns>DlnaProfile.</returns>
        DlnaProfile GetProfile(string friendlyName, string modelName, string modelNumber);
    }
}
