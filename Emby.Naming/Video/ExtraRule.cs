#pragma warning disable CS1591

using MediaBrowser.Model.Entities;
using MediaType = Emby.Naming.Common.MediaType;

namespace Emby.Naming.Video
{
    /// <summary>
    /// A rule used to match a file path with an <see cref="MediaBrowser.Model.Entities.ExtraType"/>.
    /// </summary>
    public class ExtraRule
    {
        /// <summary>
        /// Gets or sets the token to use for matching against the file path.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the type of the extra to return when matched.
        /// </summary>
        public ExtraType ExtraType { get; set; }

        /// <summary>
        /// Gets or sets the type of the rule.
        /// </summary>
        public ExtraRuleType RuleType { get; set; }

        /// <summary>
        /// Gets or sets the type of the media to return when matched.
        /// </summary>
        public MediaType MediaType { get; set; }
    }
}
