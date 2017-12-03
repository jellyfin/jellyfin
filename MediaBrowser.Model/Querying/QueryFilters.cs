using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Querying
{
    public class QueryFiltersLegacy
    {
        public string[] Genres { get; set; }
        public string[] Tags { get; set; }
        public string[] OfficialRatings { get; set; }
        public int[] Years { get; set; }

        public QueryFiltersLegacy()
        {
            Genres = new string[] { };
            Tags = new string[] { };
            OfficialRatings = new string[] { };
            Years = new int[] { };
        }
    }
    public class QueryFilters
    {
        public NameIdPair[] Genres { get; set; }
        public string[] Tags { get; set; }

        public QueryFilters()
        {
            Tags = new string[] { };
            Genres = new NameIdPair[] { };
        }
    }
}
