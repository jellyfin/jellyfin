using System;

namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class Country
    {
        public string Iso_3166_1 { get; set; }
        public string Certification { get; set; }
        public DateTime Release_Date { get; set; }
    }
}
