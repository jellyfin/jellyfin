using System;
using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class SeriesResult
    {
        public string Backdrop_Path { get; set; }
        public List<CreatedBy> Created_By { get; set; }
        public List<int> Episode_Run_Time { get; set; }
        public DateTime First_Air_Date { get; set; }
        public List<Genre> Genres { get; set; }
        public string Homepage { get; set; }
        public int Id { get; set; }
        public bool In_Production { get; set; }
        public List<string> Languages { get; set; }
        public DateTime Last_Air_Date { get; set; }
        public string Name { get; set; }
        public List<Network> Networks { get; set; }
        public int Number_Of_Episodes { get; set; }
        public int Number_Of_Seasons { get; set; }
        public string Original_Name { get; set; }
        public List<string> Origin_Country { get; set; }
        public string Overview { get; set; }
        public string Popularity { get; set; }
        public string Poster_Path { get; set; }
        public List<Season> Seasons { get; set; }
        public string Status { get; set; }
        public double Vote_Average { get; set; }
        public int Vote_Count { get; set; }
        public Credits Credits { get; set; }
        public Images Images { get; set; }
        public Keywords Keywords { get; set; }
        public ExternalIds External_Ids { get; set; }
        public General.Videos Videos { get; set; }
        public ContentRatings Content_Ratings { get; set; }
        public string ResultLanguage { get; set; }
    }
}
