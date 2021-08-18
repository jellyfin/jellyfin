#nullable disable

#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Controller.Channels
{
    public class InternalChannelItemQuery
    {
        public string FolderId { get; set; }

        public Guid UserId { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }

        public ChannelItemSortField? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }
}
