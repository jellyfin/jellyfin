using System;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class EpisodeResult
    {
        public DateTime air_date { get; set; }
        public int episode_number { get; set; }
        public string name { get; set; }
        public string overview { get; set; }
        public int id { get; set; }
        public object production_code { get; set; }
        public int season_number { get; set; }
        public string still_path { get; set; }
        public double vote_average { get; set; }
        public int vote_count { get; set; }
        public StillImages images { get; set; }
        public ExternalIds external_ids { get; set; }
        public EpisodeCredits credits { get; set; }
        public Tmdb.Models.General.Videos videos { get; set; }
    }
}
