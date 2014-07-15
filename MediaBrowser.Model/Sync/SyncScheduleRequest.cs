using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncScheduleRequest
    {
        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        public List<string> DeviceIds { get; set; }
        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public SyncQuality Quality { get; set; }

        public SyncScheduleRequest()
        {
            DeviceIds = new List<string>();
        }
    }
}
