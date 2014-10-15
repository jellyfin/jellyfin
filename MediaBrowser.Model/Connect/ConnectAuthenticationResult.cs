
namespace MediaBrowser.Model.Connect
{
    public class ConnectAuthenticationResult
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; set; }
    }
}
