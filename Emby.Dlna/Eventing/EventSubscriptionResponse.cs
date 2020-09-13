using System.Collections.Generic;

namespace Emby.Dlna.Eventing
{
    /// <summary>
    /// Contains the <see cref="EventSubscriptionResponse"/> class.
    /// </summary>
    public class EventSubscriptionResponse
    {
        /// <summary>
        /// Gets or sets the content of the response.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Gets the Headers in the response.
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
    }
}
