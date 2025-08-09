#nullable disable

#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CS1591

using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Querying
{
    public class QueryFilters
    {
        public QueryFilters()
        {
            Tags = [];
            Genres = [];
        }

        public NameGuidPair[] Genres { get; set; }

        public string[] Tags { get; set; }
    }
}
