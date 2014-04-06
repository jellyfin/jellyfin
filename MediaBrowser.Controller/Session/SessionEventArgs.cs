using System;

namespace MediaBrowser.Controller.Session
{
    public class SessionEventArgs : EventArgs
    {
        public SessionInfo SessionInfo { get; set; }
    }
}
