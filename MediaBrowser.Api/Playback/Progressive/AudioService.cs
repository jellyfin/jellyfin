using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using ServiceStack;
using System.Collections.Generic;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class GetAudioStream
    /// </summary>
    [Route("/Audio/{Id}/stream.mp3", "GET")]
    [Route("/Audio/{Id}/stream.wma", "GET")]
    [Route("/Audio/{Id}/stream.aac", "GET")]
    [Route("/Audio/{Id}/stream.flac", "GET")]
    [Route("/Audio/{Id}/stream.ogg", "GET")]
    [Route("/Audio/{Id}/stream.oga", "GET")]
    [Route("/Audio/{Id}/stream.webm", "GET")]
    [Route("/Audio/{Id}/stream", "GET")]
    [Route("/Audio/{Id}/stream.mp3", "HEAD")]
    [Route("/Audio/{Id}/stream.wma", "HEAD")]
    [Route("/Audio/{Id}/stream.aac", "HEAD")]
    [Route("/Audio/{Id}/stream.flac", "HEAD")]
    [Route("/Audio/{Id}/stream.ogg", "HEAD")]
    [Route("/Audio/{Id}/stream.oga", "HEAD")]
    [Route("/Audio/{Id}/stream.webm", "HEAD")]
    [Route("/Audio/{Id}/stream", "HEAD")]
    [Api(Description = "Gets an audio stream")]
    public class GetAudioStream : StreamRequest
    {

    }

    /// <summary>
    /// Class AudioService
    /// </summary>
    public class AudioService : BaseProgressiveStreamingService
    {
        public AudioService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IItemRepository itemRepo, IDtoService dtoService, IImageProcessor imageProcessor, IFileSystem fileSystem)
            : base(appPaths, userManager, libraryManager, isoManager, mediaEncoder, itemRepo, dtoService, imageProcessor, fileSystem)
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
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="performSubtitleConversions">if set to <c>true</c> [perform subtitle conversions].</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool performSubtitleConversions)
        {
            var request = state.Request;

            var audioTranscodeParams = new List<string>();

            var bitrate = GetAudioBitrateParam(state);

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(UsCulture));
            }

            var channels = GetNumAudioChannelsParam(request, state.AudioStream);

            if (channels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + channels.Value);
            }
            
            if (request.AudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + request.AudioSampleRate.Value);
            }

            const string vn = " -vn";

            return string.Format("{0} -i {1}{2} -threads 0{5} {3} -id3v2_version 3 -write_id3v1 1 \"{4}\"",
                GetFastSeekCommandLineParameter(request),
                GetInputArgument(state.Item, state.IsoMount),
                GetSlowSeekCommandLineParameter(request),
                string.Join(" ", audioTranscodeParams.ToArray()),
                outputPath,
                vn).Trim();
        }
    }
}
