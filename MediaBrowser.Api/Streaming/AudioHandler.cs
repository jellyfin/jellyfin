using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Providers a progressive streaming audio api
    /// </summary>
    public class AudioHandler : BaseProgressiveStreamingHandler<Audio>
    {
        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return EntityResolutionHelper.AudioFileExtensions.Any(a => ApiService.IsApiUrlMatch("audio" + a, request));
        }

        /// <summary>
        /// Gets the audio codec.
        /// </summary>
        /// <value>The audio codec.</value>
        /// <exception cref="InvalidOperationException"></exception>
        protected override AudioCodecs? AudioCodec
        {
            get
            {
                var ext = OutputFileExtension;

                if (ext.Equals(".aac", StringComparison.OrdinalIgnoreCase) || ext.Equals(".m4a", StringComparison.OrdinalIgnoreCase))
                {
                    return AudioCodecs.Aac;
                }
                if (ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    return AudioCodecs.Mp3;
                }
                if (ext.Equals(".wma", StringComparison.OrdinalIgnoreCase))
                {
                    return AudioCodecs.Wma;
                }
                if (ext.Equals(".oga", StringComparison.OrdinalIgnoreCase) || ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
                {
                    return AudioCodecs.Vorbis;
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        /// <exception cref="InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        /// <exception cref="ArgumentException">Only aac and mp3 audio codecs are supported.</exception>
        protected override string GetCommandLineArguments(string outputPath, IIsoMount isoMount)
        {
            var audioTranscodeParams = new List<string>();

            var outputFormat = AudioCodec;

            if (outputFormat != AudioCodecs.Aac && outputFormat != AudioCodecs.Mp3)
            {
                throw new InvalidOperationException("Only aac and mp3 audio codecs are supported.");
            }

            if (AudioBitRate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + AudioBitRate.Value);
            }

            var channels = GetNumAudioChannelsParam();

            if (channels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + channels.Value);
            }

            var sampleRate = GetSampleRateParam();

            if (sampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + sampleRate.Value);
            }

            const string vn = " -vn";

            return string.Format("{0} -i {1}{2} -threads 0{5} {3} -id3v2_version 3 -write_id3v1 1 \"{4}\"",
                FastSeekCommandLineParameter,
                GetInputArgument(isoMount),
                SlowSeekCommandLineParameter,
                string.Join(" ", audioTranscodeParams.ToArray()),
                outputPath,
                vn).Trim();
        }
    }
}
