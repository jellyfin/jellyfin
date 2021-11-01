namespace Emby.Naming.Video
{
    /// <summary>
    /// Data holder class for 3D format rule.
    /// </summary>
    public class Format3DRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Format3DRule"/> class.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <param name="precedingToken">Token present before current token.</param>
        public Format3DRule(string token, string? precedingToken = null)
        {
            Token = token;
            PrecedingToken = precedingToken;
        }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>The token.</value>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the preceding token.
        /// </summary>
        /// <value>The preceding token.</value>
        public string? PrecedingToken { get; set; }
    }
}
