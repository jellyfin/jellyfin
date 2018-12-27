
namespace MediaBrowser.Model.Connect
{
    public class ConnectAuthenticationResult
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public ConnectUser User { get; set; }
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; set; }
    }
}
