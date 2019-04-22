using System.Collections.Generic;
using Jellyfin.Common.Configuration;
using Jellyfin.Model.LiveTv;

namespace Jellyfin.Server.Implementations.LiveTv
{
    public class LiveTvConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
            {
                new ConfigurationStore
                {
                     ConfigurationType = typeof(LiveTvOptions),
                     Key = "livetv"
                }
            };
        }
    }
}
