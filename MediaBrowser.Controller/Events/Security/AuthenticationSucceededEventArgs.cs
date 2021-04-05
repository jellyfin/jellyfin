using Jellyfin.Data.Events;
using MediaBrowser.Controller.Authentication;

namespace MediaBrowser.Controller.Events.Security
{
    /// <summary>
    /// An event that occurs when authentication succeeds.
    /// </summary>
    public class AuthenticationSucceededEventArgs : GenericEventArgs<AuthenticationResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationSucceededEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The authentication result.</param>
        public AuthenticationSucceededEventArgs(AuthenticationResult arg) : base(arg)
        {
        }
    }
}
