using Jellyfin.Data.Events;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Events.Session
{
    /// <summary>
    /// An event that occurs on session activity.
    /// </summary>
    public class SessionActivityEventArgs : GenericEventArgs<SessionInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionActivityEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The session info.</param>
        public SessionActivityEventArgs(SessionInfo arg) : base(arg)
        {
        }
    }
}
