using System.Collections.Generic;
using Jellyfin.Common.Configuration;
using Jellyfin.Model.Branding;

namespace Jellyfin.Server.Implementations.Branding
{
    public class BrandingConfigurationFactory : IConfigurationFactory
    {
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
