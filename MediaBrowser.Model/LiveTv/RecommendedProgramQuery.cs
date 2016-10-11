using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.LiveTv
{
    public class RecommendedProgramQuery
    {
        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }
        public bool? EnableImages { get; set; }
        public int? ImageTypeLimit { get; set; }
        public ImageType[] EnableImageTypes { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        public RecommendedProgramQuery()
        {
            EnableTotalRecordCount = true;
        }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is airing.
        /// </summary>
        /// <value><c>true</c> if this instance is airing; otherwise, <c>false</c>.</value>
        public bool? IsAiring { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has aired.
        /// </summary>
        /// <value><c>null</c> if [has aired] contains no value, <c>true</c> if [has aired]; otherwise, <c>false</c>.</value>
        public bool? HasAired { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>null</c> if [is movie] contains no value, <c>true</c> if [is movie]; otherwise, <c>false</c>.</value>
        public bool? IsNews { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>null</c> if [is movie] contains no value, <c>true</c> if [is movie]; otherwise, <c>false</c>.</value>
        public bool? IsMovie { get; set; }
        public bool? IsSeries { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>null</c> if [is kids] contains no value, <c>true</c> if [is kids]; otherwise, <c>false</c>.</value>
        public bool? IsKids { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>null</c> if [is sports] contains no value, <c>true</c> if [is sports]; otherwise, <c>false</c>.</value>
        public bool? IsSports { get; set; }
    }
}