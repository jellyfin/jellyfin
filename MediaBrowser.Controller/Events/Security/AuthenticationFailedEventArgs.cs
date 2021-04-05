using Jellyfin.Data.Events;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Events.Security
{
    /// <summary>
    /// An event that occurs when authentication fails.
    /// </summary>
    public class AuthenticationFailedEventArgs : GenericEventArgs<AuthenticationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationFailedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The authentication request.</param>
        public AuthenticationFailedEventArgs(AuthenticationRequest arg) : base(arg)
        {
        }
    }
}
