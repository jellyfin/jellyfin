using System.IO;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using System;
using ServiceStack.ServiceHost;

namespace MediaBrowser.Api.Playback.Hls
{
    [Route("/Videos/{Id}/stream.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetHlsVideoStream : VideoStreamRequest
    {

    }

    [Route("/Videos/{Id}/segments/{SegmentId}/stream.ts", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
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
            var file = SegmentFilePrefix + request.SegmentId + Path.GetExtension(RequestContext.PathInfo);

            file = Path.Combine(ApplicationPaths.EncodedMediaCachePath, file);

            return ResultFactory.GetStaticFileResult(RequestContext, file);
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
            var codec = GetAudioCodec(state.Request);

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return "-codec:a:0 copy";
            }

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

                var volParam = string.Empty;

                // Boost volume to 200% when downsampling from 6ch to 2ch
                if (channels.HasValue && channels.Value <= 2 && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5)
                {
                    volParam = ",volume=2.000000";
                }
                
                args += string.Format(" -af \"aresample=async=1000{0}\"", volParam);

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
            var framerate = state.VideoRequest.Framerate ?? 0;

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

            framerate = Math.Round(framerate);

            args += string.Format(" -r {0}", framerate);

            // Needed to ensure segments stay under 10 seconds
            args += string.Format(" -g {0}", framerate);

            args += " -vsync vfr";

            if (!string.IsNullOrEmpty(state.VideoRequest.Profile))
            {
                args += " -profile:v " + state.VideoRequest.Profile;
            }

            if (!string.IsNullOrEmpty(state.VideoRequest.Level))
            {
                args += " -level 3 " + state.VideoRequest.Level;
            }
            
            if (state.SubtitleStream != null)
            {
                // This is for internal graphical subs
                if (!state.SubtitleStream.IsExternal && (state.SubtitleStream.Codec.IndexOf("pgs", StringComparison.OrdinalIgnoreCase) != -1 || state.SubtitleStream.Codec.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    args += GetInternalGraphicalSubtitleParam(state, codec);
                }
            }
         
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
