
namespace MediaBrowser.Model.Connect
{
    public class ConnectAuthenticationExchangeResult
    {
        /// <summary>
        /// Gets or sets the local user identifier.
        /// </summary>
        /// <value>The local user identifier.</value>
        public string LocalUserId { get; set; }
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; set; }
    }
}
