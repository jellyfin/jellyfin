using Jellyfin.Data.Events;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Events.Session
{
    /// <summary>
    /// An event that occurs when the session's capabilities are changed.
    /// </summary>
    public class SessionCapabilitiesChangedEventArgs : GenericEventArgs<SessionInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionCapabilitiesChangedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The session info.</param>
        public SessionCapabilitiesChangedEventArgs(SessionInfo arg) : base(arg)
        {
        }
    }
}
