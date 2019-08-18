using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.People
{
    public class PersonResult
    {
        public bool adult { get; set; }
        public List<string> also_known_as { get; set; }
        public string biography { get; set; }
        public string birthday { get; set; }
        public string deathday { get; set; }
        public string homepage { get; set; }
        public int id { get; set; }
        public string imdb_id { get; set; }
        public string name { get; set; }
        public string place_of_birth { get; set; }
        public double popularity { get; set; }
        public string profile_path { get; set; }
        public PersonImages images { get; set; }
        public ExternalIds external_ids { get; set; }
    }
}
