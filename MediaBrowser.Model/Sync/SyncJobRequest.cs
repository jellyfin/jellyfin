using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncJobRequest
    {
        /// <summary>
        /// Gets or sets the target identifier.
        /// </summary>
        /// <value>The target identifier.</value>
        public string TargetId { get; set; }
        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        public List<string> ItemIds { get; set; }
        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public SyncQuality Quality { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [unwatched only].
        /// </summary>
        /// <value><c>true</c> if [unwatched only]; otherwise, <c>false</c>.</value>
        public bool UnwatchedOnly { get; set; }
        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>The limit.</value>
        public long? Limit { get; set; }
        /// <summary>
        /// Gets or sets the type of the limit.
        /// </summary>
        /// <value>The type of the limit.</value>
        public SyncLimitType? LimitType { get; set; }

        public SyncJobRequest()
        {
            ItemIds = new List<string>();
        }
    }

    public enum SyncLimitType
    {
        ItemCount = 0
    }
}
