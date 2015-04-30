using System;

namespace MediaBrowser.Controller.Dlna
{
    public interface ISsdpHandler
    {
        event EventHandler<SsdpMessageEventArgs> MessageReceived;
    }
}
