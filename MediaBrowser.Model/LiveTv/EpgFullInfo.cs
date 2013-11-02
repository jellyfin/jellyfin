using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class EpgFullInfo
    {
        /// <summary>
        /// ChannelId for the EPG.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// List of all the programs for a specific channel
        /// </summary>
        public List<EpgInfo> EpgInfos { get; set; } 
    }
}
