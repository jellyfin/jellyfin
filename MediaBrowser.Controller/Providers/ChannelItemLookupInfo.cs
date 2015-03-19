using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class ChannelItemLookupInfo : ItemLookupInfo
    {
        public ChannelMediaContentType ContentType { get; set; }
        public ExtraType ExtraType { get; set; }
    }
}