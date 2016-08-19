
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
        /// Gets or sets the target identifier.
        /// </summary>
        /// <value>The target identifier.</value>
        public string TargetId { get; set; }
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
        public string ExcludeTargetIds { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SyncJobStatus[] Statuses { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [synchronize new content].
        /// </summary>
        /// <value><c>null</c> if [synchronize new content] contains no value, <c>true</c> if [synchronize new content]; otherwise, <c>false</c>.</value>
        public bool? SyncNewContent { get; set; }

        public SyncJobQuery()
        {
            Statuses = new SyncJobStatus[] { };
        }
    }
}
