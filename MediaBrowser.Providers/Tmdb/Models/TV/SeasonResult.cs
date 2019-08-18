using System;
using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class SeasonResult
    {
        public DateTime Air_Date { get; set; }
        public List<Episode> Episodes { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public int Id { get; set; }
        public string Poster_Path { get; set; }
        public int Season_Number { get; set; }
        public Credits Credits { get; set; }
        public SeasonImages Images { get; set; }
        public ExternalIds External_Ids { get; set; }
        public General.Videos Videos { get; set; }
    }
}
