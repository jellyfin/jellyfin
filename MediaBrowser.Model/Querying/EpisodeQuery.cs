
namespace MediaBrowser.Model.Querying
{
    public class EpisodeQuery
    {
        public string UserId { get; set; }

        public string SeriesId { get; set; }

        public bool? IsMissing { get; set; }

        public bool? IsVirtualUnaired { get; set; }

        public int? SeasonNumber { get; set; }

        public ItemFields[] Fields { get; set; }

        public EpisodeQuery()
        {
            Fields = new ItemFields[] { };
        }
    }

    public class SeasonQuery
    {
        public string UserId { get; set; }

        public string SeriesId { get; set; }

        public bool? IsMissing { get; set; }

        public bool? IsVirtualUnaired { get; set; }

        public ItemFields[] Fields { get; set; }

        public bool? IsSpecialSeason { get; set; }

        public SeasonQuery()
        {
            Fields = new ItemFields[] { };
        }
    }
}
