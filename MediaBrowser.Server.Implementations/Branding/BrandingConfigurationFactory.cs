using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Branding;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Branding
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
