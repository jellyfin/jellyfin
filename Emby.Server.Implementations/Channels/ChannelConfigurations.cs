using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using System.Collections.Generic;

namespace Emby.Server.Implementations.Channels
{
    public static class ChannelConfigurationExtension
    {
        public static ChannelOptions GetChannelsConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<ChannelOptions>("channels");
        }
    }

    public class ChannelConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                    Key = "channels",
                    ConfigurationType = typeof (ChannelOptions)
                }
            };
        }
    }
}
