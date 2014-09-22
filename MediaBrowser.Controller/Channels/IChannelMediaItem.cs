using System.Collections.Generic;
using MediaBrowser.Model.Channels;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelMediaItem : IChannelItem
    {
        long? RunTimeTicks { get; set; }
        string MediaType { get; }

        ChannelMediaContentType ContentType { get; set; }

        List<ChannelMediaInfo> ChannelMediaSources { get; set; }
    }
}