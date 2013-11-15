using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class ChannelGuide
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }

        /// <summary>
        /// ChannelId for the EPG.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// List of all the programs for a specific channel
        /// </summary>
        public List<ProgramInfo> Programs { get; set; } 
    }
}
