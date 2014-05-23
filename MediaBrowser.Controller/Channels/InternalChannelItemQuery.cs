using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public class InternalChannelItemQuery
    {
        public string FolderId { get; set; }

        public User User { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }

    public class InternalAllChannelItemsQuery
    {
        public User User { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }

}