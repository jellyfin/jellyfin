#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Notifications;

namespace Emby.Notifications
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
                    ConfigurationType = typeof(NotificationOptions)
                }
            };
        }
    }
}
