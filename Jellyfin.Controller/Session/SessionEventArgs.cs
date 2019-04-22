using System;

namespace Jellyfin.Controller.Session
{
    public class SessionEventArgs : EventArgs
    {
        public SessionInfo SessionInfo { get; set; }
    }
}
