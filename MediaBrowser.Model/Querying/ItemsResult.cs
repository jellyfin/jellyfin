using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Represents the result of a query for items
    /// </summary>
    public class ItemsResult
    {
        /// <summary>
        /// The set of items returned based on sorting, paging, etc
        /// </summary>
        /// <value>The items.</value>
        public BaseItemDto[] Items { get; set; }

        /// <summary>
        /// The total number of records available
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsResult"/> class.
        /// </summary>
        public ItemsResult()
        {
            Items = new BaseItemDto[] { };
        }
    }
}
