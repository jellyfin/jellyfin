using System;

namespace MediaBrowser.Controller.Events.Session
{
    /// <summary>
    /// An event that fires when a session is ended.
    /// </summary>
    public class SessionEndedEventArgs : EventArgs
    {
        /// <summary>
        /// The user's name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The user's id.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The device's name.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// The remote endpoint.
        /// </summary>
        public string RemoteEndPoint { get; set; }
    }
}
