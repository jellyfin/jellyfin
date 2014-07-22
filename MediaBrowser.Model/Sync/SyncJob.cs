
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
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string ItemId { get; set; }
        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public SyncQuality Quality { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SyncJobStatus Status { get; set; }
        /// <summary>
        /// Gets or sets the current progress.
        /// </summary>
        /// <value>The current progress.</value>
        public double? CurrentProgress { get; set; }
        /// <summary>
        /// Gets or sets the synchronize rule identifier.
        /// </summary>
        /// <value>The synchronize rule identifier.</value>
        public string SyncScheduleId { get; set; }
        /// <summary>
        /// Gets or sets the transcoded path.
        /// </summary>
        /// <value>The transcoded path.</value>
        public string TranscodedPath { get; set; }
    }
}
