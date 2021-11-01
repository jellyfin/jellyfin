#pragma warning disable CS1591

using Emby.Dlna.Configuration;
using MediaBrowser.Common.Configuration;

namespace Emby.Dlna
{
    public static class ConfigurationExtension
    {
        public static DlnaOptions GetDlnaConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<DlnaOptions>("dlna");
        }
    }
}
