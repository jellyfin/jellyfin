using System;

namespace Emby.Dlna.PlayTo
{
    public class TransportStateEventArgs : EventArgs
    {
        public TRANSPORTSTATE State { get; set; }
    }
}
