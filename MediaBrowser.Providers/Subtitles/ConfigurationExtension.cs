using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;

namespace MediaBrowser.Providers.Subtitles
{
    public static class ConfigurationExtension
    {
        public static SubtitleOptions GetSubtitleConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<SubtitleOptions>("subtitles");
        }
    }

    public class SubtitleConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                    Key = "subtitles",
                    ConfigurationType = typeof (SubtitleOptions)
                }
            };
        }
    }
}
