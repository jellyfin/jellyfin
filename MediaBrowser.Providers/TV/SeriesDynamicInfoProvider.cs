using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class SeriesDynamicInfoProvider : BaseMetadataProvider, IDynamicInfoProvider
    {
        public SeriesDynamicInfoProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            var series = (Series)item;

            var episodes = series.RecursiveChildren
                .OfType<Episode>()
                .ToList();

            series.DateLastEpisodeAdded = episodes.Select(i => i.DateCreated)
                .OrderByDescending(i => i)
                .FirstOrDefault();

            // Don't save to the db
            return FalseTaskResult;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }
    }
}
