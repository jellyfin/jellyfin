using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeasonIdentityProvider : IItemIdentityProvider<SeasonInfo>
    {
        public static readonly string FullIdKey = MetadataProviders.Tvdb + "-Full";

        public Task Identify(SeasonInfo info)
        {
            string tvdbSeriesId;
            if (!info.SeriesProviderIds.TryGetValue(MetadataProviders.Tvdb.ToString(), out tvdbSeriesId) || string.IsNullOrEmpty(tvdbSeriesId) || info.IndexNumber == null)
            {
                return Task.FromResult<object>(null);
            }

            if (string.IsNullOrEmpty(info.GetProviderId(FullIdKey)))
            {
                var id = string.Format("{0}:{1}", tvdbSeriesId, info.IndexNumber.Value);
                info.SetProviderId(FullIdKey, id);
            }
            
            return Task.FromResult<object>(null);
        }

        public static TvdbSeasonIdentity? ParseIdentity(string id)
        {
            if (id == null)
            {
                return null;
            }

            try
            {
                var parts = id.Split(':');
                return new TvdbSeasonIdentity(parts[0], int.Parse(parts[1]));
            }
            catch
            {
                return null;
            }
        }
    }

    public struct TvdbSeasonIdentity
    {
        public string SeriesId { get; private set; }
        public int Index { get; private set; }

        public TvdbSeasonIdentity(string id)
            : this()
        {
            this = TvdbSeasonIdentityProvider.ParseIdentity(id).Value;
        }

        public TvdbSeasonIdentity(string seriesId, int index)
            : this()
        {
            SeriesId = seriesId;
            Index = index;
        }
    }
}