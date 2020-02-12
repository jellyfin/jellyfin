using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

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

    public class EncodingConfigurationStore : ConfigurationStore, IValidatingConfiguration
    {
        public EncodingConfigurationStore()
        {
            ConfigurationType = typeof(EncodingOptions);
            Key = "encoding";
        }

        public void Validate(object oldConfig, object newConfig)
        {
            var newPath = ((EncodingOptions)newConfig).TranscodingTempPath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(((EncodingOptions)oldConfig).TranscodingTempPath, newPath, StringComparison.Ordinal))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} does not exist.",
                            newPath));
                }
            }
        }
    }
}
