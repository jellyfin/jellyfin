
namespace MediaBrowser.Model.Querying
{
    public class QueryResult<T>
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public T[] Items { get; set; }

        /// <summary>
        /// The total number of records available
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; set; }

        public QueryResult()
        {
            Items = new T[] { };
        }
    }
}
