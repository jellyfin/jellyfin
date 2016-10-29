using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class TransportStateEventArgs : EventArgs
    {
        public TRANSPORTSTATE State { get; set; }
    }
}
