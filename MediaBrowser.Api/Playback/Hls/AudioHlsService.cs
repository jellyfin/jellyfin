using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using System;

namespace MediaBrowser.Api.Playback.Hls
{
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
        public AudioHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager)
            : base(appPaths, userManager, libraryManager)
        {
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

            throw new InvalidOperationException("Only aac and mp3 audio codecs are supported.");
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
