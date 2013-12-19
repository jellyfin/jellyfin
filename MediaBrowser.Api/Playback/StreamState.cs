using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Api.Playback
{
    public class StreamState
    {
        public string RequestedUrl { get; set; }

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

        /// <summary>
        /// Gets or sets the iso mount.
        /// </summary>
        /// <value>The iso mount.</value>
        public IIsoMount IsoMount { get; set; }

        public string MediaPath { get; set; }

        public bool IsRemote { get; set; }

        public bool IsInputVideo { get; set; }

        public VideoType VideoType { get; set; }

        public IsoType? IsoType { get; set; }

        public List<string> PlayableStreamFileNames { get; set; }
    }
}
