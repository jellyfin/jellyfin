#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Branding;

namespace Emby.Server.Implementations.Branding
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
