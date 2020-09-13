#nullable enable

using MediaBrowser.Common.Configuration;

namespace Emby.Dlna.Configuration
{
    public static class ConfigurationExtension
    {
        public static DlnaOptions GetDlnaConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<DlnaOptions>("dlna");
        }
    }
}
