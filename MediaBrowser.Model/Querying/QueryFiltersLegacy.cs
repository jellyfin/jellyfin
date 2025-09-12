#nullable disable

#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CS1591

namespace MediaBrowser.Model.Querying
{
    public class QueryFiltersLegacy
    {
        public QueryFiltersLegacy()
        {
            Genres = [];
            Tags = [];
            OfficialRatings = [];
            Years = [];
        }

        public string[] Genres { get; set; }

        public string[] Tags { get; set; }

        public string[] OfficialRatings { get; set; }

        public int[] Years { get; set; }
    }
}
