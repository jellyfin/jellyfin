using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class TransportStateEventArgs : EventArgs
    {
        public bool Stopped { get; set; }
    }
}
