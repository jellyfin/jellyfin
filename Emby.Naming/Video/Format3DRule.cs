#pragma warning disable CS1591

namespace Emby.Naming.Video
{
    public class Format3DRule
    {
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
