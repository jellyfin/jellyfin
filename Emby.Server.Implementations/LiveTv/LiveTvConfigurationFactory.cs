using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.LiveTv;
using System.Collections.Generic;

namespace Emby.Server.Implementations.LiveTv
{
    public class LiveTvConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
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
