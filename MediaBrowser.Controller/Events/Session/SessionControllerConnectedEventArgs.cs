using Jellyfin.Data.Events;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Events.Session
{
    /// <summary>
    /// An event that occurs when a session controller is connected.
    /// </summary>
    public class SessionControllerConnectedEventArgs : GenericEventArgs<SessionInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionControllerConnectedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The session info.</param>
        public SessionControllerConnectedEventArgs(SessionInfo arg) : base(arg)
        {
        }
    }
}
