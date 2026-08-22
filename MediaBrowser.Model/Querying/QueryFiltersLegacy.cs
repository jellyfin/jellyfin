#nullable disable
#pragma warning disable CS1591

using System.Collections.Generic;

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

        public IReadOnlyList<string> Genres { get; set; }

        public IReadOnlyList<string> Tags { get; set; }

        public IReadOnlyList<string> OfficialRatings { get; set; }

        public IReadOnlyList<int> Years { get; set; }
    }
}
