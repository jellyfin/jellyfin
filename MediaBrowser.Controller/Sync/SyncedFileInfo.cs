using System.Collections.Generic;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.Sync
{
    public class SyncedFileInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        public string[] PathParts { get; set; }
        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        public MediaProtocol Protocol { get; set; }
        /// <summary>
        /// Gets or sets the required HTTP headers.
        /// </summary>
        /// <value>The required HTTP headers.</value>
        public Dictionary<string, string> RequiredHttpHeaders { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        public SyncedFileInfo()
        {
            RequiredHttpHeaders = new Dictionary<string, string>();
        }
    }
}
