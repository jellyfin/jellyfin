using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class MovieResult
    {
        public bool adult { get; set; }
        public string backdrop_path { get; set; }
        public BelongsToCollection belongs_to_collection { get; set; }
        public int budget { get; set; }
        public List<Genre> genres { get; set; }
        public string homepage { get; set; }
        public int id { get; set; }
        public string imdb_id { get; set; }
        public string original_title { get; set; }
        public string original_name { get; set; }
        public string overview { get; set; }
        public double popularity { get; set; }
        public string poster_path { get; set; }
        public List<ProductionCompany> production_companies { get; set; }
        public List<ProductionCountry> production_countries { get; set; }
        public string release_date { get; set; }
        public int revenue { get; set; }
        public int runtime { get; set; }
        public List<SpokenLanguage> spoken_languages { get; set; }
        public string status { get; set; }
        public string tagline { get; set; }
        public string title { get; set; }
        public string name { get; set; }
        public double vote_average { get; set; }
        public int vote_count { get; set; }
        public Casts casts { get; set; }
        public Releases releases { get; set; }
        public Images images { get; set; }
        public Keywords keywords { get; set; }
        public Trailers trailers { get; set; }

        public string GetOriginalTitle()
        {
            return original_name ?? original_title;
        }

        public string GetTitle()
        {
            return name ?? title ?? GetOriginalTitle();
        }
    }
}
