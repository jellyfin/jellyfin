using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Api.Playback
{
    public class StreamState
    {
        public string RequestedUrl { get; set; }

        public StreamRequest Request { get; set; }

        public VideoStreamRequest VideoRequest
        {
            get { return Request as VideoStreamRequest; }
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

        public bool HasMediaStreams { get; set; }

        public bool SendInputOverStandardInput { get; set; }

        public CancellationTokenSource StandardInputCancellationTokenSource { get; set; }

        public string LiveTvStreamId { get; set; }

        public int SegmentLength = 10;
        public int HlsListSize;

        public long? RunTimeTicks;

        public string AudioSync = "1";
        public string VideoSync = "vfr";

        public bool DeInterlace { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        public string InputFormat { get; set; }

        public string InputVideoCodec { get; set; }

        public string InputAudioCodec { get; set; }
    }
}
