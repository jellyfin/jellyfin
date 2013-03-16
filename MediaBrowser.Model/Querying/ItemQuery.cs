using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Contains all the possible parameters that can be used to query for items
    /// </summary>
    public class ItemQuery
    {
        /// <summary>
        /// The user to localize search results for
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        public string ParentId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// What to sort the results by
        /// </summary>
        /// <value>The sort by.</value>
        public string[] SortBy { get; set; }

        /// <summary>
        /// The sort order to return results with
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Filters to apply to the results
        /// </summary>
        /// <value>The filters.</value>
        public ItemFilter[] Filters { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        /// <summary>
        /// Whether or not to perform the query recursively
        /// </summary>
        /// <value><c>true</c> if recursive; otherwise, <c>false</c>.</value>
        public bool Recursive { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        public string[] Genres { get; set; }

        /// <summary>
        /// Limit results to items containing specific studios
        /// </summary>
        /// <value>The studios.</value>
        public string[] Studios { get; set; }

        /// <summary>
        /// Gets or sets the exclude item types.
        /// </summary>
        /// <value>The exclude item types.</value>
        public string[] ExcludeItemTypes { get; set; }

        /// <summary>
        /// Gets or sets the include item types.
        /// </summary>
        /// <value>The include item types.</value>
        public string[] IncludeItemTypes { get; set; }
        
        /// <summary>
        /// Limit results to items containing specific years
        /// </summary>
        /// <value>The years.</value>
        public int[] Years { get; set; }

        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        public string Person { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        public string PersonType { get; set; }

        /// <summary>
        /// Search characters used to find items
        /// </summary>
        /// <value>The index by.</value>
        public string SearchTerm { get; set; }
        
        /// <summary>
        /// The dynamic, localized index function name
        /// </summary>
        /// <value>The index by.</value>
        public string IndexBy { get; set; }

        /// <summary>
        /// Gets or sets the image types.
        /// </summary>
        /// <value>The image types.</value>
        public ImageType[] ImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the ids, which are specific items to retrieve
        /// </summary>
        /// <value>The ids.</value>
        public string[] Ids { get; set; }
    }
}
