using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;

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
    /// Class AudioHlsService
    /// </summary>
    public class AudioHlsService : BaseHlsService
    {
        public AudioHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService, IFileSystem fileSystem, IItemRepository itemRepository)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, dtoService, fileSystem, itemRepository)
        {
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
