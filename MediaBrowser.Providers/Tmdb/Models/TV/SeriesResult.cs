using System;
using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class SeriesResult
    {
        public string backdrop_path { get; set; }
        public List<CreatedBy> created_by { get; set; }
        public List<int> episode_run_time { get; set; }
        public DateTime first_air_date { get; set; }
        public List<Genre> genres { get; set; }
        public string homepage { get; set; }
        public int id { get; set; }
        public bool in_production { get; set; }
        public List<string> languages { get; set; }
        public DateTime last_air_date { get; set; }
        public string name { get; set; }
        public List<Network> networks { get; set; }
        public int number_of_episodes { get; set; }
        public int number_of_seasons { get; set; }
        public string original_name { get; set; }
        public List<string> origin_country { get; set; }
        public string overview { get; set; }
        public string popularity { get; set; }
        public string poster_path { get; set; }
        public List<Season> seasons { get; set; }
        public string status { get; set; }
        public double vote_average { get; set; }
        public int vote_count { get; set; }
        public Credits credits { get; set; }
        public Images images { get; set; }
        public Keywords keywords { get; set; }
        public ExternalIds external_ids { get; set; }
        public General.Videos videos { get; set; }
        public ContentRatings content_ratings { get; set; }
        public string ResultLanguage { get; set; }
    }
}
