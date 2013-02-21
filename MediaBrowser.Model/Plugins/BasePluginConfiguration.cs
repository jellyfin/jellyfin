using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// Class BasePluginConfiguration
    /// </summary>
    public class BasePluginConfiguration
    {
        /// <summary>
        /// Whether or not this plug-in should be automatically updated when a
        /// compatible new version is released
        /// </summary>
        /// <value><c>true</c> if [enable auto update]; otherwise, <c>false</c>.</value>
        public bool EnableAutoUpdate { get; set; }

        /// <summary>
        /// The classification of updates to which to subscribe.
        /// Options are: Dev, Beta or Release
        /// </summary>
        /// <value>The update class.</value>
        public PackageVersionClass UpdateClass { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePluginConfiguration" /> class.
        /// </summary>
        public BasePluginConfiguration()
        {
            EnableAutoUpdate = true;
        }
    }
}
