using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelMediaItem : IChannelItem
    {
        long? RunTimeTicks { get; set; }
        string MediaType { get; }

        ChannelMediaContentType ContentType { get; set; }

        ExtraType ExtraType { get; set; }

        List<ChannelMediaInfo> ChannelMediaSources { get; set; }
    }
}