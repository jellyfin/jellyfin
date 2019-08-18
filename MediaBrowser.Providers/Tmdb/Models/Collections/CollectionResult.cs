using System.Collections.Generic;

namespace MediaBrowser.Providers.Tmdb.Models.Collections
{
    public class CollectionResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string Poster_Path { get; set; }
        public string Backdrop_Path { get; set; }
        public List<Part> Parts { get; set; }
        public CollectionImages Images { get; set; }
    }
}
