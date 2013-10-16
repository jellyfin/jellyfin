using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Model.Querying
{
    public class NextUpQuery
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

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
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        /// <summary>
        /// Gets or sets the exclude location types.
        /// </summary>
        /// <value>The exclude location types.</value>
        public LocationType[] ExcludeLocationTypes { get; set; }

        public bool? HasPremiereDate { get; set; }
        public DateTime? MinPremiereDate { get; set; }
        public DateTime? MaxPremiereDate { get; set; }

        public NextUpQuery()
        {
            ExcludeLocationTypes = new LocationType[] { };
        }
    }
}
