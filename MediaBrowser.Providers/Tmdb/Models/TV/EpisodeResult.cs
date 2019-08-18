using System;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class EpisodeResult
    {
        public DateTime Air_Date { get; set; }
        public int Episode_Number { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public int Id { get; set; }
        public object Production_Code { get; set; }
        public int Season_Number { get; set; }
        public string Still_Path { get; set; }
        public double Vote_Average { get; set; }
        public int Vote_Count { get; set; }
        public StillImages Images { get; set; }
        public ExternalIds External_Ids { get; set; }
        public EpisodeCredits Credits { get; set; }
        public Tmdb.Models.General.Videos Videos { get; set; }
    }
}
