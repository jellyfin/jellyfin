using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.LiveTv;

namespace Emby.Server.Implementations.LiveTv
{
    /// <summary>
    /// <see cref="IConfigurationFactory" /> implementation for <see cref="LiveTvOptions" />.
    /// </summary>
    public class LiveTvConfigurationFactory : IConfigurationFactory
    {
        /// <inheritdoc />
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
