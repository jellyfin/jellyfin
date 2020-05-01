#pragma warning disable CS1591

namespace Emby.Naming.TV
{
    public class EpisodePathParserResult
    {
        public int? SeasonNumber { get; set; }

        public int? EpisodeNumber { get; set; }

        public int? EndingEpsiodeNumber { get; set; }

        public string SeriesName { get; set; }

        public bool Success { get; set; }

        public bool IsByDate { get; set; }

        public int? Year { get; set; }

        public int? Month { get; set; }

        public int? Day { get; set; }
    }
}
