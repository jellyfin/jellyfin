using System.Collections.Generic;

namespace MediaBrowser.Providers.Tmdb.Models.Collections
{
    public class CollectionResult
    {
        public int id { get; set; }
        public string name { get; set; }
        public string overview { get; set; }
        public string poster_path { get; set; }
        public string backdrop_path { get; set; }
        public List<Part> parts { get; set; }
        public CollectionImages images { get; set; }
    }
}
