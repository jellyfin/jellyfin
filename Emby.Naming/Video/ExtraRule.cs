#pragma warning disable CS1591
#pragma warning disable SA1600

using MediaBrowser.Model.Entities;
using MediaType = Emby.Naming.Common.MediaType;

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
        public ExtraType ExtraType { get; set; }

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
