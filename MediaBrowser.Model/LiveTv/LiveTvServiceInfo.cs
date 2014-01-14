using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ServiceInfo
    /// </summary>
    public class LiveTvServiceInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    public class GuideInfo
    {
        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime EndDate { get; set; }
    }

    public class LiveTvInfo
    {
        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        /// <value>The services.</value>
        public List<LiveTvServiceInfo> Services { get; set; }

        /// <summary>
        /// Gets or sets the name of the active service.
        /// </summary>
        /// <value>The name of the active service.</value>
        public string ActiveServiceName { get; set; }

        public LiveTvInfo()
        {
            Services = new List<LiveTvServiceInfo>();
        }
    }
}
