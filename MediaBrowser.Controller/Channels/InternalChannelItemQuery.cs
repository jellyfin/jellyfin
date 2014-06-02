using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Controller.Channels
{
    public class InternalChannelItemQuery
    {
        public string FolderId { get; set; }

        public User User { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }

        public ChannelItemSortField? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }

    public class InternalAllChannelMediaQuery
    {
        public User User { get; set; }
    }
}