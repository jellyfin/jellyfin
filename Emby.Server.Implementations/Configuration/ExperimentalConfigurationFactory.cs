using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace Emby.Server.Implementations.Configuration
{
    /// <summary>
    /// A configuration factory for <see cref="ExperimentalConfiguration"/>.
    /// </summary>
    public class ExperimentalConfigurationFactory : IConfigurationFactory
    {
        /// <inheritdoc />
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return
            [
                new ConfigurationStore
                {
                    ConfigurationType = typeof(ExperimentalConfiguration),
                    Key = "experimental"
                }
            ];
        }
    }
}
