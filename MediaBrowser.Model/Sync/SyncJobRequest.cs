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
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public string Quality { get; set; }
        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string Profile { get; set; }
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
        /// Gets or sets a value indicating whether [synchronize new content].
        /// </summary>
        /// <value><c>true</c> if [synchronize new content]; otherwise, <c>false</c>.</value>
        public bool SyncNewContent { get; set; }
        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>The limit.</value>
        public int? ItemLimit { get; set; }
        /// <summary>
        /// Gets or sets the bitrate.
        /// </summary>
        /// <value>The bitrate.</value>
        public int? Bitrate { get; set; }

        public SyncJobRequest()
        {
            ItemIds = new List<string>();
            SyncNewContent = true;
        }
    }
}
