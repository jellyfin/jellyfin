using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncJob
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        public string TargetId { get; set; }
        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public SyncQuality Quality { get; set; }
        /// <summary>
        /// Gets or sets the current progress.
        /// </summary>
        /// <value>The current progress.</value>
        public double? Progress { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SyncJobStatus Status { get; set; }        
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
        /// <summary>
        /// Gets or sets the requested item ids.
        /// </summary>
        /// <value>The requested item ids.</value>
        public List<string> RequestedItemIds { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is dynamic.
        /// </summary>
        /// <value><c>true</c> if this instance is dynamic; otherwise, <c>false</c>.</value>
        public bool IsDynamic { get; set; }
        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        public DateTime DateCreated { get; set; }
        /// <summary>
        /// Gets or sets the date last modified.
        /// </summary>
        /// <value>The date last modified.</value>
        public DateTime DateLastModified { get; set; }
        /// <summary>
        /// Gets or sets the item count.
        /// </summary>
        /// <value>The item count.</value>
        public int ItemCount { get; set; }

        public string ParentName { get; set; }
        public string PrimaryImageItemId { get; set; }
        public string PrimaryImageTag { get; set; }
        public double? PrimaryImageAspectRatio { get; set; }

        public SyncJob()
        {
            RequestedItemIds = new List<string>();
        }
    }
}
