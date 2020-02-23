#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.LiveTv;

namespace Emby.Server.Implementations.LiveTv
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
