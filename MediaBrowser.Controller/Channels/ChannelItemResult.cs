#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelItemResult
    {
        public ChannelItemResult()
        {
            Items = Array.Empty<ChannelItemInfo>();
        }

        public IReadOnlyList<ChannelItemInfo> Items { get; set; }

        public int? TotalRecordCount { get; set; }
    }
}
