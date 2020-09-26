using MediaBrowser.Common.Configuration;

namespace Emby.Dlna.Configuration
{
    /// <summary>
    /// Exntension method for <see cref="IConfigurationManager"/>.
    /// </summary>
    public static class ConfigurationExtension
    {
        /// <summary>
        /// Returns the DlnaConfiguration key.
        /// </summary>
        /// <param name="manager">The <see cref="IConfigurationManager"/> instance.</param>
        /// <returns>A <see cref="DlnaOptions"/> containing the dlna configuration.</returns>
        public static DlnaOptions GetDlnaConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<DlnaOptions>("dlna");
        }
    }
}
