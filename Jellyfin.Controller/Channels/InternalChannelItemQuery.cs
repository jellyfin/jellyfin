using System;
using Jellyfin.Model.Channels;


namespace Jellyfin.Controller.Channels
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
