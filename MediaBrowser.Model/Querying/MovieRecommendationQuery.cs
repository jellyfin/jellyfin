#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Querying
{
    public class MovieRecommendationQuery
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the parent identifier.
        /// </summary>
        /// <value>The parent identifier.</value>
        public string ParentId { get; set; }

        /// <summary>
        /// Gets or sets the item limit.
        /// </summary>
        /// <value>The item limit.</value>
        public int ItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the category limit.
        /// </summary>
        /// <value>The category limit.</value>
        public int CategoryLimit { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        public MovieRecommendationQuery()
        {
            ItemLimit = 10;
            CategoryLimit = 6;
            Fields = Array.Empty<ItemFields>();
        }
    }
}
