namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// The result of a successful authentication by an <see cref="IAuthenticationProvider"/>.
    /// </summary>
    public class ProviderAuthenticationResult
    {
        /// <summary>
        /// Gets or sets the authenticated user name.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }
    }
}
