using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Controller.Configuration
{
    public class ServerConfiguration : BaseConfiguration
    {
        public string ImagesByNamePath { get; set; }

        /// <summary>
        /// Gets or sets the default UI configuration
        /// </summary>
        public UserConfiguration DefaultUserConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a list of registered UI device names
        /// </summary>
        public List<string> DeviceNames { get; set; }

        /// <summary>
        /// Gets or sets all available UIConfigurations
        /// The key contains device name and user id
        /// </summary>
        public Dictionary<string, UserConfiguration> UserConfigurations { get; set; }

        public ServerConfiguration()
            : base()
        {
            DefaultUserConfiguration = new UserConfiguration();

            UserConfigurations = new Dictionary<string, UserConfiguration>();

            DeviceNames = new List<string>();
        }
    }
}
