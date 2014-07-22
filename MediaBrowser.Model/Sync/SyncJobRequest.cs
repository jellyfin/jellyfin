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

        public SyncJobRequest()
        {
            TargetIds = new List<string>();
            ItemIds = new List<string>();
        }
    }
}
