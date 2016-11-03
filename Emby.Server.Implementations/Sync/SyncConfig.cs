using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;

namespace Emby.Server.Implementations.Sync
{
    public class SyncConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                     ConfigurationType = typeof(SyncOptions),
                     Key = "sync"
                }
            };
        }
    }

    public static class SyncExtensions
    {
        public static SyncOptions GetSyncOptions(this IConfigurationManager config)
        {
            return config.GetConfiguration<SyncOptions>("sync");
        }
    }
}
