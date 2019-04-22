using System.Collections.Generic;
using Jellyfin.Common.Configuration;
using Jellyfin.Model.Notifications;

namespace Jellyfin.Notifications
{
    public class NotificationConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
            {
                new ConfigurationStore
                {
                    Key = "notifications",
                    ConfigurationType = typeof (NotificationOptions)
                }
            };
        }
    }
}
