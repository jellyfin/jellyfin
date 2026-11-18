#nullable disable

using System.ComponentModel.DataAnnotations;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// A command to display a message on a client.
    /// </summary>
    public class MessageCommand
    {
        /// <summary>
        /// Gets or sets the message header.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds after which the message should be dismissed.
        /// </summary>
        public long? TimeoutMs { get; set; }
    }
}
