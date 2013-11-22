using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Querying
{
    public class EpisodeQuery
    {
        public string UserId { get; set; }

        public string SeriesId { get; set; }

        public LocationType[] ExcludeLocationTypes { get; set; }

        public int? SeasonNumber { get; set; }

        public ItemFields[] Fields { get; set; }

        public EpisodeQuery()
        {
            Fields = new ItemFields[] { };
            ExcludeLocationTypes = new LocationType[] { };
        }
    }
}
