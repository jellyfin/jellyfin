using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    public class ExtraRule
    {
        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>The token.</value>
        public string Token { get; set; }
        /// <summary>
        /// Gets or sets the type of the extra.
        /// </summary>
        /// <value>The type of the extra.</value>
        public string ExtraType { get; set; }
        /// <summary>
        /// Gets or sets the type of the rule.
        /// </summary>
        /// <value>The type of the rule.</value>
        public ExtraRuleType RuleType { get; set; }
        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public MediaType MediaType { get; set; }
    }
}
