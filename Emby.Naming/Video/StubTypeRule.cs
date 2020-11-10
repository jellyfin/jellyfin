namespace Emby.Naming.Video
{
    public class StubTypeRule
    {
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
