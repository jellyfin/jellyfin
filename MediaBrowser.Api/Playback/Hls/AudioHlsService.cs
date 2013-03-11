using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using ServiceStack.ServiceHost;
using System;
using System.IO;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class GetHlsAudioStream
    /// </summary>
    [Route("/Audio/{Id}/stream.m3u8", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets an audio stream using HTTP live streaming.")]
    public class GetHlsAudioStream : StreamRequest
    {

    }

    [Route("/Audio/{Id}/segments/{SegmentId}/stream.mp3", "GET")]
    [Route("/Audio/{Id}/segments/{SegmentId}/stream.aac", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsAudioSegment
    {
        public string Id { get; set; }

        public string SegmentId { get; set; }
    }

    /// <summary>
    /// Class AudioHlsService
    /// </summary>
    public class AudioHlsService : BaseHlsService
    {
        public AudioHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager)
            : base(appPaths, userManager, libraryManager, isoManager)
        {
        }

        public object Get(GetHlsAudioSegment request)
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
        public object Get(GetHlsAudioStream request)
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

            var args = "-codec:a " + codec;

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

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments(StreamState state)
        {
            // No video
            return string.Empty;
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            if (state.Request.AudioCodec == AudioCodecs.Aac)
            {
                return ".aac";
            }
            if (state.Request.AudioCodec == AudioCodecs.Mp3)
            {
                return ".mp3";
            }

            throw new ArgumentException("Must specify either aac or mp3 audio codec.");
        }

        /// <summary>
        /// Gets the map args.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetMapArgs(StreamState state)
        {
            return string.Format("-map 0:{0}", state.AudioStream.Index);
        }
    }
}
