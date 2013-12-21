using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers
{
    public class VirtualItemImageValidator : BaseMetadataProvider
    {
        public VirtualItemImageValidator(ILogManager logManager, IServerConfigurationManager configurationManager) 
            : base(logManager, configurationManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            var locationType = item.LocationType;

            return locationType == LocationType.Virtual ||
                   locationType == LocationType.Remote;
        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            item.ValidateImages();
            item.ValidateBackdrops();

            var hasScreenshots = item as IHasScreenshots;

            if (hasScreenshots != null)
            {
                hasScreenshots.ValidateScreenshots();
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
