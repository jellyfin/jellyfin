
namespace MediaBrowser.Model.Querying
{
    public class QueryFilters
    {
        public string[] Genres { get; set; }
        public string[] Tags { get; set; }
        public string[] OfficialRatings { get; set; }
        public int[] Years { get; set; }

        public QueryFilters()
        {
            Genres = new string[] { };
            Tags = new string[] { };
            OfficialRatings = new string[] { };
            Years = new int[] { };
        }
    }
}
