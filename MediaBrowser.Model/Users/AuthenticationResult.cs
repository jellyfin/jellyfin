using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Users
{
    public class AuthenticationResult
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public UserDto User { get; set; }

        /// <summary>
        /// Gets or sets the session information.
        /// </summary>
        /// <value>The session information.</value>
        public SessionInfoDto SessionInfo { get; set; }
    }
}
