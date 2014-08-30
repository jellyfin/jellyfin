using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelFactory
    {
        IEnumerable<IChannel> GetChannels();
    }

    public interface IFactoryChannel
    {
        
    }
}