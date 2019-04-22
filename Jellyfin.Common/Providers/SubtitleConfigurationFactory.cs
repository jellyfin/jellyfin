using System.Collections.Generic;
using Jellyfin.Common.Configuration;
using Jellyfin.Model.Providers;

namespace Jellyfin.Common.Providers
{
    public class SubtitleConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            yield return new ConfigurationStore()
            {
                Key = "subtitles",
                ConfigurationType = typeof(SubtitleOptions)
            };
        }
    }
}
