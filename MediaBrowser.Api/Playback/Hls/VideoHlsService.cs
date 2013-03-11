using System.IO;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using System;
using ServiceStack.ServiceHost;

namespace MediaBrowser.Api.Playback.Hls
{
    [Route("/Videos/{Id}/stream.m3u8", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetHlsVideoStream : VideoStreamRequest
    {

    }

    [Route("/Videos/{Id}/segments/{SegmentId}/stream.ts", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsVideoSegment
    {
        public string Id { get; set; }

        public string SegmentId { get; set; }
    }
    
    public class VideoHlsService : BaseHlsService
    {
        public VideoHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager)
            : base(appPaths, userManager, libraryManager, isoManager)
        {
        }

        public object Get(GetHlsVideoSegment request)
        {
            var file = SegmentFilePrefix + request.SegmentId + Path.GetExtension(Request.PathInfo);

            file = Path.Combine(ApplicationPaths.EncodedMediaCachePath, file);

            return ToStaticFileResult(file);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetHlsVideoStream request)
        {
            return ProcessRequest(request);
        }
        
        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetAudioArguments(StreamState state)
        {
            if (!state.Request.AudioCodec.HasValue)
            {
                return "-codec:a:0 copy";
            }

            var codec = GetAudioCodec(state.Request);

            var args = "-codec:a:0 " + codec;

            if (state.AudioStream != null)
            {
                var channels = GetNumAudioChannelsParam(state.Request, state.AudioStream);

                if (channels.HasValue)
                {
                    args += " -ac " + channels.Value;
                }

                if (state.Request.AudioSampleRate.HasValue)
                {
                    args += " -ar " + state.Request.AudioSampleRate.Value;
                }

                if (state.Request.AudioBitRate.HasValue)
                {
                    args += " -ab " + state.Request.AudioBitRate.Value;
                }

                return args;
            }

            return args;
        }

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments(StreamState state)
        {
            var codec = GetVideoCodec(state.VideoRequest);

            // Right now all we support is either h264 or copy
            if (!codec.Equals("copy", StringComparison.OrdinalIgnoreCase) && !codec.Equals("libx264", StringComparison.OrdinalIgnoreCase))
            {
                codec = "libx264";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return IsH264(state.VideoStream) ? "-codec:v:0 copy -bsf h264_mp4toannexb" : "-codec:v:0 copy";
            }

            var args = "-codec:v:0 " + codec + " -preset superfast";

            if (state.VideoRequest.VideoBitRate.HasValue)
            {
                args += string.Format(" -b:v {0}", state.VideoRequest.VideoBitRate.Value);
            }

            // Add resolution params, if specified
            if (state.VideoRequest.Width.HasValue || state.VideoRequest.Height.HasValue || state.VideoRequest.MaxHeight.HasValue || state.VideoRequest.MaxWidth.HasValue)
            {
                args += GetOutputSizeParam(state, codec);
            }

            // Get the output framerate based on the FrameRate param
            double framerate = state.VideoRequest.Framerate ?? 0;

            // We have to supply a framerate for hls, so if it's null, account for that here
            if (framerate.Equals(0))
            {
                framerate = state.VideoStream.AverageFrameRate ?? 0;
            }
            if (framerate.Equals(0))
            {
                framerate = state.VideoStream.RealFrameRate ?? 0;
            }
            if (framerate.Equals(0))
            {
                framerate = 23.976;
            }

            args += string.Format(" -r {0}", framerate);

            // Needed to ensure segments stay under 10 seconds
            args += string.Format(" -g {0}", framerate);

            return args;
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            return ".ts";
        }
    }
}
