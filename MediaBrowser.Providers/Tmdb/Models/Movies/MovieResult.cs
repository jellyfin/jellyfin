using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class MovieResult
    {
        public bool Adult { get; set; }
        public string Backdrop_Path { get; set; }
        public BelongsToCollection Belongs_To_Collection { get; set; }
        public int Budget { get; set; }
        public List<Genre> Genres { get; set; }
        public string Homepage { get; set; }
        public int Id { get; set; }
        public string Imdb_Id { get; set; }
        public string Original_Title { get; set; }
        public string Original_Name { get; set; }
        public string Overview { get; set; }
        public double Popularity { get; set; }
        public string Poster_Path { get; set; }
        public List<ProductionCompany> Production_Companies { get; set; }
        public List<ProductionCountry> Production_Countries { get; set; }
        public string Release_Date { get; set; }
        public int Revenue { get; set; }
        public int Runtime { get; set; }
        public List<SpokenLanguage> Spoken_Languages { get; set; }
        public string Status { get; set; }
        public string Tagline { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public double Vote_Average { get; set; }
        public int Vote_Count { get; set; }
        public Casts Casts { get; set; }
        public Releases Releases { get; set; }
        public Images Images { get; set; }
        public Keywords Keywords { get; set; }
        public Trailers Trailers { get; set; }

        public string GetOriginalTitle()
        {
            return Original_Name ?? Original_Title;
        }

        public string GetTitle()
        {
            return Name ?? Title ?? GetOriginalTitle();
        }
    }
}
