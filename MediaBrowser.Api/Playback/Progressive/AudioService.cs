using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using ServiceStack;
using System.Collections.Generic;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class GetAudioStream
    /// </summary>
    [Route("/Audio/{Id}/stream.{Container}", "GET", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream", "GET", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream.{Container}", "HEAD", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream", "HEAD", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream.{Container}", "OPTIONS", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream", "OPTIONS", Summary = "Gets an audio stream")]
    public class GetAudioStream : StreamRequest
    {
        [ApiMember(Name = "Container", Description = "Container", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Container { get; set; }
    }

    /// <summary>
    /// Class AudioService
    /// </summary>
    public class AudioService : BaseProgressiveStreamingService
    {
        public AudioService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder, IImageProcessor imageProcessor, IHttpClient httpClient) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder, imageProcessor, httpClient)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAudioStream request)
        {
            return ProcessRequest(request, false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Head(GetAudioStream request)
        {
            return ProcessRequest(request, true);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Options(GetAudioStream request)
        {
            return ProcessRequest(request, true);
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="isEncoding">if set to <c>true</c> [is encoding].</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
        {
            var audioTranscodeParams = new List<string>();

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(UsCulture));
            }

            if (state.OutputAudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + state.OutputAudioChannels.Value.ToString(UsCulture));
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + state.OutputAudioSampleRate.Value.ToString(UsCulture));
            }

            const string vn = " -vn";

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            return string.Format("{0} -i {1} -threads {2}{3} {4} -id3v2_version 3 -write_id3v1 1 -y \"{5}\"",
                inputModifier,
                GetInputArgument(state),
                threads,
                vn,
                string.Join(" ", audioTranscodeParams.ToArray()),
                outputPath).Trim();
        }
    }
}
