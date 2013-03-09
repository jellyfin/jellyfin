using System.IO;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.Playback
{
    public class StreamState
    {
        public string Url { get; set; }

        public StreamRequest Request { get; set; }

        public VideoStreamRequest VideoRequest
        {
            get { return (VideoStreamRequest) Request; }
        }
        
        /// <summary>
        /// Gets or sets the log file stream.
        /// </summary>
        /// <value>The log file stream.</value>
        public Stream LogFileStream { get; set; }

        public MediaStream AudioStream { get; set; }

        public MediaStream VideoStream { get; set; }

        public MediaStream SubtitleStream { get; set; }

        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the iso mount.
        /// </summary>
        /// <value>The iso mount.</value>
        public IIsoMount IsoMount { get; set; }
    }
}
