
namespace MediaBrowser.Model.Sync
{
    public class SyncJobItemQuery
    {
        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }
        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }
        /// <summary>
        /// Gets or sets the job identifier.
        /// </summary>
        /// <value>The job identifier.</value>
        public string JobId { get; set; }
        /// <summary>
        /// Gets or sets the target identifier.
        /// </summary>
        /// <value>The target identifier.</value>
        public string TargetId { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SyncJobItemStatus? Status { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is completed.
        /// </summary>
        /// <value><c>null</c> if [is completed] contains no value, <c>true</c> if [is completed]; otherwise, <c>false</c>.</value>
        public bool? IsCompleted { get; set; }
    }
}
