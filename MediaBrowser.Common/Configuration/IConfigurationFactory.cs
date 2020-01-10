#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.Configuration
{
    public interface IConfigurationFactory
    {
        IEnumerable<ConfigurationStore> GetConfigurations();
    }

    public class ConfigurationStore
    {
        public string Key { get; set; }

        public Type ConfigurationType { get; set; }
    }

    public interface IValidatingConfiguration
    {
        void Validate(object oldConfig, object newConfig);
    }
}
