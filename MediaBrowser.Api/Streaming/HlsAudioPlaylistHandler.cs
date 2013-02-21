using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Dto;
using System;
using System.ComponentModel.Composition;
using System.Net;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Class HlsAudioPlaylistHandler
    /// </summary>
    [Export(typeof(IHttpServerHandler))]
    public class HlsAudioPlaylistHandler : BaseHlsPlaylistHandler<Audio>
    {
        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("audio.m3u8", request);
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <value>The segment file extension.</value>
        /// <exception cref="InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        protected override string SegmentFileExtension
        {
            get
            {
                if (AudioCodec == AudioCodecs.Aac)
                {
                    return ".aac";
                }
                if (AudioCodec == AudioCodecs.Mp3)
                {
                    return ".mp3";
                }

                throw new InvalidOperationException("Only aac and mp3 audio codecs are supported.");
            }
        }

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetVideoArguments()
        {
            // No video
            return string.Empty;
        }

        /// <summary>
        /// Gets the map args.
        /// </summary>
        /// <value>The map args.</value>
        protected override string MapArgs
        {
            get
            {
                return string.Format("-map 0:{0}", AudioStream.Index);
            }
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetAudioArguments()
        {
            var codec = GetAudioCodec();

            var args = "-codec:a " + codec;

            var channels = GetNumAudioChannelsParam();

            if (channels.HasValue)
            {
                args += " -ac " + channels.Value;
            }

            var sampleRate = GetSampleRateParam();

            if (sampleRate.HasValue)
            {
                args += " -ar " + sampleRate.Value;
            }

            if (AudioBitRate.HasValue)
            {
                args += " -ab " + AudioBitRate.Value;
            }

            return args;
        }
    }
}
