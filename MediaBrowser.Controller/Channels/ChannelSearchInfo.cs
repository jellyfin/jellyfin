namespace MediaBrowser.Controller.Channels
{
    public class ChannelSearchInfo
    {
        public string SearchTerm { get; set; }

        public string UserId { get; set; }
    }

    public class ChannelLatestMediaSearch
    {
        public string UserId { get; set; }
    }
}