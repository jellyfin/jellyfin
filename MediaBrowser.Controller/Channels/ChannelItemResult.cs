using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelItemResult
    {
        public List<ChannelItemInfo> Items { get; set; }

        public int? TotalRecordCount { get; set; }
    }
}