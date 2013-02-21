using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Dlna.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the friendly name of the DLNA Server.
        /// </summary>
        /// <value>The friendly name of the DLNA Server.</value>
        public string FriendlyDlnaName { get; set; }

        /// <summary>
        /// Gets or sets the Port Number for the DLNA Server.
        /// </summary>
        /// <value>The Port Number of the DLNA Server.</value>
        public short? DlnaPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the user to impersonate.
        /// </summary>
        /// <value>The name of the User.</value>
        public string UserName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
            : base()
        {
            //this.DlnaPortNumber = 1845;
            this.FriendlyDlnaName = "MB3 UPnP";
            this.UserName = string.Empty;
        }
    }
}
