using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class InternalEncodingTask
    {
        public string Id { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public double ProgressPercentage { get; set; }

        public EncodingOptions Request { get; set; }

        public VideoEncodingOptions VideoRequest
        {
            get { return Request as VideoEncodingOptions; }
        }

        public string MediaPath { get; set; }
        public List<string> StreamFileNames { get; set; }
        public bool IsInputRemote { get; set; }

        public VideoType? InputVideoType { get; set; }
        public IsoType? IsoType { get; set; }
        public long? InputRunTimeTicks;

        public string AudioSync = "1";
        public string VideoSync = "vfr";

        public string InputAudioSync { get; set; }
        public string InputVideoSync { get; set; }

        public bool DeInterlace { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        public string InputFormat { get; set; }

        public string InputVideoCodec { get; set; }

        public string InputAudioCodec { get; set; }

        public string LiveTvStreamId { get; set; }

        public MediaStream AudioStream { get; set; }
        public MediaStream VideoStream { get; set; }
        public MediaStream SubtitleStream { get; set; }
        public bool HasMediaStreams { get; set; }

        public int SegmentLength = 10;
        public int HlsListSize;

        public string MimeType { get; set; }
        public string OrgPn { get; set; }
        public bool EnableMpegtsM2TsMode { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        /// <value>The user agent.</value>
        public string UserAgent { get; set; }

        public EncodingQuality QualitySetting { get; set; }

        public InternalEncodingTask()
        {
            Id = Guid.NewGuid().ToString("N");
            CancellationTokenSource = new CancellationTokenSource();
            StreamFileNames = new List<string>();
        }

        public bool EnableDebugLogging { get; set; }

        internal void OnBegin()
        {
            
        }

        internal void OnCompleted()
        {
            
        }

        internal void OnError()
        {
            
        }
    }
}
