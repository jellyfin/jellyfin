using System.Collections.Generic;

namespace Emby.Naming.Video
{
    public class ExtraResult
    {
        /// <summary>
        /// Gets or sets the type of the extra.
        /// </summary>
        /// <value>The type of the extra.</value>
        public string ExtraType { get; set; }
        /// <summary>
        /// Gets or sets the rule.
        /// </summary>
        /// <value>The rule.</value>
        public ExtraRule Rule { get; set; }
    }
}
