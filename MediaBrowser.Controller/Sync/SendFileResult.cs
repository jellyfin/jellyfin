using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.Sync
{
    public class SendFileResult
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }
        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        public MediaProtocol Protocol { get; set; }
    }
}
