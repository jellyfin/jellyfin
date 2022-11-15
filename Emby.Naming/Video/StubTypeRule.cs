namespace Emby.Naming.Video
{
    /// <summary>
    /// Data class holding information about Stub type rule.
    /// </summary>
    public class StubTypeRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubTypeRule"/> class.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <param name="stubType">Stub type.</param>
        public StubTypeRule(string token, string stubType)
        {
            Token = token;
            StubType = stubType;
        }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>The token.</value>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the type of the stub.
        /// </summary>
        /// <value>The type of the stub.</value>
        public string StubType { get; set; }
    }
}
