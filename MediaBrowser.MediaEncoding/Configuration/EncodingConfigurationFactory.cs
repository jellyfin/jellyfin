using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using System.Collections.Generic;
using System.IO;

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
            var oldEncodingConfig = (EncodingOptions)oldConfig;
            var newEncodingConfig = (EncodingOptions)newConfig;

            var newPath = newEncodingConfig.TranscodingTempPath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(oldEncodingConfig.TranscodingTempPath ?? string.Empty, newPath))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }
            }
        }
    }
}
