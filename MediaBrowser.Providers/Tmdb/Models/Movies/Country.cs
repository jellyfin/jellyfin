using System;

namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class Country
    {
        public string iso_3166_1 { get; set; }
        public string certification { get; set; }
        public DateTime release_date { get; set; }
    }
}
