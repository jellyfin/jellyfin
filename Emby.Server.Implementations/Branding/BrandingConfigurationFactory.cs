using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Branding;

namespace Emby.Server.Implementations.Branding
{
    /// <summary>
    /// A configuration factory for <see cref="BrandingOptions"/>.
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
