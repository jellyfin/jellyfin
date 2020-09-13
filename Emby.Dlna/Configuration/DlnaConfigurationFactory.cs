#nullable enable
using System.Collections.Generic;
using Emby.Dlna.Configuration;
using MediaBrowser.Common.Configuration;

namespace Emby.Dlna.Configuration
{
    /// <summary>
    /// Create a class for <see cref="DlnaConfigurationFactory"/>.
    /// </summary>
    public class DlnaConfigurationFactory : IConfigurationFactory
    {
        /// <summary>
        /// Returns the configuration store for the <seealso cref="DlnaOptions"/>.
        /// </summary>
        /// <returns>An enumerable <seealso cref="ConfigurationStore"/> containing the information.</returns>
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                    Key = "dlna",
                    ConfigurationType = typeof(DlnaOptions)
                }
            };
        }
    }
}
