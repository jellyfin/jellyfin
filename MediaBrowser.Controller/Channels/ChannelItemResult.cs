using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelItemResult
    {
        public List<ChannelItemInfo> Items { get; set; }

        public TimeSpan CacheLength { get; set; }
    }
}