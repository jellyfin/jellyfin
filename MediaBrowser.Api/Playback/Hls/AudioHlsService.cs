using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using ServiceStack.ServiceHost;
using System;
using System.IO;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class GetHlsAudioStream
    /// </summary>
    [Route("/Audio/{Id}/stream.m3u8", "GET")]
    [Api(Description = "Gets an audio stream using HTTP live streaming.")]
    public class GetHlsAudioStream : StreamRequest
    {

    }

    /// <summary>
    /// Class GetHlsAudioSegment
    /// </summary>
    [Route("/Audio/{Id}/hls/{SegmentId}/stream.mp3", "GET")]
    [Route("/Audio/{Id}/hls/{SegmentId}/stream.aac", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsAudioSegment
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    /// <summary>
    /// Class AudioHlsService
    /// </summary>
    public class AudioHlsService : BaseHlsService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioHlsService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        public AudioHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService)
            : base(appPaths, userManager, libraryManager, isoManager, mediaEncoder, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetHlsAudioSegment request)
        {
            var file = request.SegmentId + Path.GetExtension(RequestContext.PathInfo);

            file = Path.Combine(ApplicationPaths.EncodedMediaCachePath, file);

            return ResultFactory.GetStaticFileResult(RequestContext, file, FileShare.ReadWrite);
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
        /// <param name="performSubtitleConversion">if set to <c>true</c> [perform subtitle conversion].</param>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments(StreamState state, bool performSubtitleConversion)
        {
            // No video
            return string.Empty;
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentException">Must specify either aac or mp3 audio codec.</exception>
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
