using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.FileOrganization;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public static class ConfigurationExtension
    {
        public static AutoOrganizeOptions GetAutoOrganizeOptions(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<AutoOrganizeOptions>("autoorganize");
        }
        public static void SaveAutoOrganizeOptions(this IConfigurationManager manager, AutoOrganizeOptions options)
        {
            manager.SaveConfiguration("autoorganize", options);
        }
    }

    public class AutoOrganizeOptionsFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                    Key = "autoorganize",
                    ConfigurationType = typeof (AutoOrganizeOptions)
                }
            };
        }
    }
}
