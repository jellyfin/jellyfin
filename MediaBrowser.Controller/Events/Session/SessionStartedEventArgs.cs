using Jellyfin.Data.Events;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Events.Session
{
    /// <summary>
    /// An event that fires when a session is started.
    /// </summary>
    public class SessionStartedEventArgs : GenericEventArgs<SessionInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The session info.</param>
        public SessionStartedEventArgs(SessionInfo arg) : base(arg)
        {
        }
    }
}
