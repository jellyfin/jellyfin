#nullable disable
#pragma warning disable CS1591

using System;

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
        /// Gets or sets the name of the target.
        /// </summary>
        /// <value>The name of the target.</value>
        public string TargetName { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public string Quality { get; set; }

        /// <summary>
        /// Gets or sets the bitrate.
        /// </summary>
        /// <value>The bitrate.</value>
        public int? Bitrate { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        public SyncCategory? Category { get; set; }

        /// <summary>
        /// Gets or sets the parent identifier.
        /// </summary>
        /// <value>The parent identifier.</value>
        public string ParentId { get; set; }

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
        /// Gets or sets a value indicating whether [synchronize new content].
        /// </summary>
        /// <value><c>true</c> if [synchronize new content]; otherwise, <c>false</c>.</value>
        public bool SyncNewContent { get; set; }

        /// <summary>
        /// Gets or sets the item limit.
        /// </summary>
        /// <value>The item limit.</value>
        public int? ItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the requested item ids.
        /// </summary>
        /// <value>The requested item ids.</value>
        public Guid[] RequestedItemIds { get; set; }

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

        public SyncJob()
        {
            RequestedItemIds = Array.Empty<Guid>();
        }
    }
}
