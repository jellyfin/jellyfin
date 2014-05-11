namespace MediaBrowser.Controller.Channels
{
    public interface IChannelMediaItem : IChannelItem
    {
        bool IsInfiniteStream { get; set; }

        ChannelMediaContentType ContentType { get; set; }
    }
}