#nullable enable

using System.Collections.Generic;
using Emby.Dlna.Configuration;
using MediaBrowser.Common.Configuration;

namespace Emby.Dlna.Configuration
{
    public class DlnaConfigurationFactory : IConfigurationFactory
    {
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
