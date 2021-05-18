#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelItemResult
    {
        public ChannelItemResult()
        {
            Items = new List<ChannelItemInfo>();
        }

        public List<ChannelItemInfo> Items { get; set; }

        public int? TotalRecordCount { get; set; }
    }
}
