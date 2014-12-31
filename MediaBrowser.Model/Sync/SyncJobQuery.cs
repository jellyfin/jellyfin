
namespace MediaBrowser.Model.Sync
{
    public class SyncJobQuery
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
        /// Gets or sets a value indicating whether this instance is completed.
        /// </summary>
        /// <value><c>null</c> if [is completed] contains no value, <c>true</c> if [is completed]; otherwise, <c>false</c>.</value>
        public bool? IsCompleted { get; set; }
        /// <summary>
        /// Gets or sets the target identifier.
        /// </summary>
        /// <value>The target identifier.</value>
        public string TargetId { get; set; }
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
    }
}
