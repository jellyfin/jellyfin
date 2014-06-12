using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeasonIdentityProvider : IItemIdentityProvider<SeasonInfo, SeasonIdentity>
    {
        public Task<SeasonIdentity> FindIdentity(SeasonInfo info)
        {
            string tvdbSeriesId;
            if (!info.SeriesProviderIds.TryGetValue(MetadataProviders.Tvdb.ToString(), out tvdbSeriesId) || string.IsNullOrEmpty(tvdbSeriesId) || info.IndexNumber == null)
            {
                return Task.FromResult<SeasonIdentity>(null);
            }

            var result = new SeasonIdentity
            {
                Type = MetadataProviders.Tvdb.ToString(),
                SeriesId = tvdbSeriesId,
                SeasonIndex = info.IndexNumber.Value
            };

            return Task.FromResult(result);
        }

        public int Order { get { return 0; } }
    }
}