using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public class InternalChannelItemQuery
    {
        public string CategoryId { get; set; }

        public User User { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }
}