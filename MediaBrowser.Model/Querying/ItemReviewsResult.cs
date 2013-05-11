using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class ItemReviewsResult
    /// </summary>
    public class ItemReviewsResult
    {
        /// <summary>
        /// Gets or sets the item reviews.
        /// </summary>
        /// <value>The item reviews.</value>
        public ItemReview[] ItemReviews { get; set; }

        /// <summary>
        /// The total number of records available
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsResult" /> class.
        /// </summary>
        public ItemReviewsResult()
        {
            ItemReviews = new ItemReview[] { };
        }
   }
}
