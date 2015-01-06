using System;

namespace MediaBrowser.Model.Sync
{
    public class SyncJobItem
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the job identifier.
        /// </summary>
        /// <value>The job identifier.</value>
        public string JobId { get; set; }

        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the item.</value>
        public string ItemName { get; set; }
        
        /// <summary>
        /// Gets or sets the media source identifier.
        /// </summary>
        /// <value>The media source identifier.</value>
        public string MediaSourceId { get; set; }
        
        /// <summary>
        /// Gets or sets the target identifier.
        /// </summary>
        /// <value>The target identifier.</value>
        public string TargetId { get; set; }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        /// <value>The output path.</value>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SyncJobItemStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the current progress.
        /// </summary>
        /// <value>The current progress.</value>
        public double? Progress { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        public DateTime DateCreated { get; set; }
        /// <summary>
        /// Gets or sets the primary image item identifier.
        /// </summary>
        /// <value>The primary image item identifier.</value>
        public string PrimaryImageItemId { get; set; }
        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        public string PrimaryImageTag { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [requires conversion].
        /// </summary>
        /// <value><c>true</c> if [requires conversion]; otherwise, <c>false</c>.</value>
        public bool RequiresConversion { get; set; }
    }
}
