using System;
using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class SeasonResult
    {
        public DateTime air_date { get; set; }
        public List<Episode> episodes { get; set; }
        public string name { get; set; }
        public string overview { get; set; }
        public int id { get; set; }
        public string poster_path { get; set; }
        public int season_number { get; set; }
        public Credits credits { get; set; }
        public SeasonImages images { get; set; }
        public ExternalIds external_ids { get; set; }
        public General.Videos videos { get; set; }
    }
}
