using System;

namespace MediaBrowser.Controller.Events.Session
{
    /// <summary>
    /// An event that fires when a session is ended.
    /// </summary>
    public class SessionEndedEventArgs : EventArgs
    {
        public string UserName { get; set; }

        public Guid UserId { get; set; }

        public string DeviceName { get; set; }

        public string RemoteEndPoint { get; set; }
    }
}
