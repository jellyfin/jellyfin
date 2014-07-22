using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncJobRequest
    {
        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        public List<string> TargetIds { get; set; }
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

        public SyncJobRequest()
        {
            TargetIds = new List<string>();
        }
    }
}
