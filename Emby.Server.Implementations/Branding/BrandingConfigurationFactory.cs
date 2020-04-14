using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Branding;

namespace Emby.Server.Implementations.Branding
{
    /// <summary>
    /// Branding configuration factory.
    /// </summary>
    public class BrandingConfigurationFactory : IConfigurationFactory
    {
        /// <inheritdoc />
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                     ConfigurationType = typeof(BrandingOptions),
                     Key = "branding"
                }
            };
        }
    }
}
