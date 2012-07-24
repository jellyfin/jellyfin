using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Configuration
{
    /// <summary>
    /// Extends BaseConfigurationController by adding methods to get and set UIConfiguration data
    /// </summary>
    public class ServerConfigurationController : ConfigurationController<ServerConfiguration>
    {
        private string GetDictionaryKey(Guid userId, string deviceName)
        {
            string guidString = userId == Guid.Empty ? string.Empty : userId.ToString();

            return deviceName + "-" + guidString;
        }

        public UserConfiguration GetUserConfiguration(Guid userId)
        {
            return Configuration.DefaultUserConfiguration;
        }
    }
}
