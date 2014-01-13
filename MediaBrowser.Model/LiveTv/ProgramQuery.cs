using System;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ProgramQuery.
    /// </summary>
    public class ProgramQuery
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string[] ChannelIdList { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        public DateTime? MinStartDate { get; set; }

        public DateTime? MaxStartDate { get; set; }

        public DateTime? MinEndDate { get; set; }

        public DateTime? MaxEndDate { get; set; }
        
        public ProgramQuery()
        {
            ChannelIdList = new string[] { };
        }
    }

    public class RecommendedProgramQuery
    {
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
    }
}
