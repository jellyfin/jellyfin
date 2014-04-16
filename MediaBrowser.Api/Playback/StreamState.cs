using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Api.Playback
{
    public class StreamState : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILiveTvManager _liveTvManager;

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

        public string LiveTvStreamId { get; set; }

        public int SegmentLength = 10;
        public int HlsListSize;

        public long? RunTimeTicks;

        public string AudioSync = "1";
        public string VideoSync = "vfr";

        public List<string> SupportedAudioCodecs { get; set; }

        public StreamState(ILiveTvManager liveTvManager, ILogger logger)
        {
            _liveTvManager = liveTvManager;
            _logger = logger;
            SupportedAudioCodecs = new List<string>();
        }

        public string InputAudioSync { get; set; }
        public string InputVideoSync { get; set; }
 
        public bool DeInterlace { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        public string InputFormat { get; set; }

        public string InputVideoCodec { get; set; }

        public string InputAudioCodec { get; set; }

        public string MimeType { get; set; }
        public string OrgPn { get; set; }

        // DLNA Settings
        public bool EstimateContentLength { get; set; }
        public bool EnableMpegtsM2TsMode { get; set; }
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }
        
        public string GetMimeType(string outputPath)
        {
            if (!string.IsNullOrEmpty(MimeType))
            {
                return MimeType;
            }

            return MimeTypes.GetMimeType(outputPath);
        }

        public void Dispose()
        {
            DisposeLiveStream();
            DisposeLogStream();
            DisposeIsoMount();
        }

        private void DisposeLogStream()
        {
            if (LogFileStream != null)
            {
                try
                {
                    LogFileStream.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing log stream", ex);
                }

                LogFileStream = null;
            }
        }

        private void DisposeIsoMount()
        {
            if (IsoMount != null)
            {
                try
                {
                    IsoMount.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing iso mount", ex);
                }

                IsoMount = null;
            }
        }

        private async void DisposeLiveStream()
        {
            if (!string.IsNullOrEmpty(LiveTvStreamId))
            {
                try
                {
                    await _liveTvManager.CloseLiveStream(LiveTvStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing live tv stream", ex);
                }
            }
        }
    }
}
