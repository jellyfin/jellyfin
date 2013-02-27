using MediaBrowser.Controller;
using ServiceStack.ServiceHost;
using System.Collections.Generic;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class GetAudioStream
    /// </summary>
    [Route("/Audio/{Id}.mp3", "GET")]
    [Route("/Audio/{Id}.wma", "GET")]
    [Route("/Audio/{Id}.aac", "GET")]
    [Route("/Audio/{Id}.flac", "GET")]
    [Route("/Audio/{Id}.ogg", "GET")]
    [Route("/Audio/{Id}", "GET")]
    public class GetAudioStream : StreamRequest
    {

    }

    /// <summary>
    /// Class AudioService
    /// </summary>
    public class AudioService : BaseProgressiveStreamingService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseProgressiveStreamingService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        public AudioService(IServerApplicationPaths appPaths)
            : base(appPaths)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAudioStream request)
        {
            return ProcessRequest(request);
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">Only aac and mp3 audio codecs are supported.</exception>
        protected override string GetCommandLineArguments(string outputPath, StreamState state)
        {
            var request = state.Request;

            var audioTranscodeParams = new List<string>();

            if (request.AudioBitRate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + request.AudioBitRate.Value);
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
