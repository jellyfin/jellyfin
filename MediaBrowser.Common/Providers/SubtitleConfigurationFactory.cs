#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Common.Providers
{
    public class SubtitleConfigurationFactory : IConfigurationFactory
    {
        /// <inheritdoc />
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
