#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace MediaBrowser.MediaEncoding.Configuration
{
    public class EncodingConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new EncodingConfigurationStore()
            };
        }
    }
}
