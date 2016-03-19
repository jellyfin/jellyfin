using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelItem : IHasImages, IHasTags
    {
        string ChannelId { get; set; }

        string ExternalId { get; set; }
    }
}
