#nullable disable

#pragma warning disable CS1591

using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Library
{
    public static class MetadataConfigurationExtensions
    {
        public static MetadataConfiguration GetMetadataConfiguration(this IConfigurationManager config)
        {
            return config.GetConfiguration<MetadataConfiguration>("metadata");
        }
    }
}
