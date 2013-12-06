using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers
{
    public class UserRootFolderNameProvider : BaseMetadataProvider
    {
        public UserRootFolderNameProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            return item is UserRootFolder;
        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            var parentName = Path.GetFileNameWithoutExtension(item.Path);

            if (string.Equals(parentName, "default", StringComparison.OrdinalIgnoreCase))
            {
                item.Name = "Media Library";
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return TrueTaskResult;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }
    }
}
